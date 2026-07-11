using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using RagPipeline;

namespace AgentOrchestrator;

public class AgentAnalyzeService(
    IConfiguration config,
    Func<LlmConfig, IChatClient> chatClientFactory,
    IRagService ragService,
    ILogger<AgentAnalyzeService> logger) : IAnalyzeService
{
    // Small LLMs sometimes invent plausible-but-wrong parameter names instead of
    // using the tool's actual schema. Map common variants to the real names.
    private static readonly Dictionary<string, Dictionary<string, string>> ParamAliases = new()
    {
        ["get_error_logs"] = new()
        {
            ["date_from"] = "from", ["start_date"] = "from", ["start"] = "from", ["fromDate"] = "from",
            ["date_to"] = "to", ["end_date"] = "to", ["end"] = "to", ["toDate"] = "to",
            ["error_type"] = "errorType", ["errorType"] = "errorType",
        },
        ["get_recent_deployments"] = new()
        {
            ["environment"] = "env",
            ["deployment_date"] = "date", ["target_date"] = "date", ["timestamp"] = "date",
        },
    };

    private static AIFunctionArguments? NormalizeArguments(string toolName, IDictionary<string, object?>? arguments)
    {
        if (arguments is null)
            return null;

        if (!ParamAliases.TryGetValue(toolName, out var aliases))
            return new AIFunctionArguments(arguments);

        var normalized = new Dictionary<string, object?>();
        foreach (var (key, value) in arguments)
        {
            var canonical = aliases.GetValueOrDefault(key, key);
            normalized[canonical] = value;
        }

        return new AIFunctionArguments(normalized);
    }

    public async Task<string> AnalyzeAsync(string question, LlmConfig llmConfig, CancellationToken ct = default)
    {
        var chatClient = chatClientFactory(llmConfig);
        IReadOnlyList<string> codeChunks = [];
        try
        {
            codeChunks = await ragService.RetrieveAsync(question, topK: 1, ct);
            logger.LogInformation("RAG retrieval successful: {Count}", codeChunks.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RAG retrieval failed — proceeding without code context.");
        }

        var codeContext = codeChunks.Count > 0
            ? string.Join("\n\n---\n\n", codeChunks)
            : "(no relevant source code context found)";

        await using var mcpClient = await McpClient.CreateAsync(
            new StdioClientTransport(new StdioClientTransportOptions
            {
                Command = ResolveMcpPath(config["McpServer:ExecutablePath"])
            }),
            cancellationToken: ct);

        var mcpTools = await mcpClient.ListToolsAsync(cancellationToken: ct);
        logger.LogInformation("MCP tools loaded: {Count}", mcpTools.Count);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, $"""
                You are an expert error log analyzer.

                Your task:
                1. Call tools to fetch recent error logs.
                2. Correlate stack traces with deployed source code.
                3. Identify the exact root cause.

                Do not only summarize errors.
                Always explain:
                - failing file
                - failing line
                - why it failed
                - likely fix

                Current date: {DateTime.UtcNow:yyyy-MM-dd}.

                Relevant source code:
                {codeContext}
                """),
            new(ChatRole.User, question)
        };

        var options = new ChatOptions { Tools = [.. mcpTools], MaxOutputTokens = 120, Temperature = 0.1f };
        int round = 0;

        while (true)
        {
            round++;
            logger.LogInformation("[Round {Round}] Calling LLM with {MessageCount} messages", round, messages.Count);

            var response = await chatClient.GetResponseAsync(messages, options, ct);
            messages.AddRange(response.Messages);

            var toolCalls = response.Messages
                .SelectMany(m => m.Contents.OfType<FunctionCallContent>())
                .ToList();

            if (toolCalls.Count == 0)
            {
                logger.LogInformation("[Round {Round}] No tool calls — agent done", round);
                return response.Text ?? "";
            }

            foreach (var call in toolCalls)
            {
                logger.LogInformation("[Round {Round}] Tool call: {Name} args={Args}", round, call.Name, call.Arguments);

                var tool = mcpTools.FirstOrDefault(t => t.Name == call.Name);
                if (tool is null)
                {
                    logger.LogWarning("[Round {Round}] Unknown tool: {Name}", round, call.Name);
                    messages.Add(new ChatMessage(ChatRole.Tool,
                        [new FunctionResultContent(call.CallId, $"Tool '{call.Name}' not found.")]));
                    continue;
                }

                var result = await tool.InvokeAsync(NormalizeArguments(call.Name, call.Arguments), ct);
                var resultText = result?.ToString() ?? "";

                logger.LogInformation("[Round {Round}] Tool result: {Name} → {Preview}",
                    round, call.Name, resultText.Length > 200 ? resultText[..200] + "…" : resultText);

                messages.Add(new ChatMessage(ChatRole.Tool,
                    [new FunctionResultContent(call.CallId, result)]));
            }
        }
    }

    private string ResolveMcpPath(string? configured)
    {
        if (!string.IsNullOrEmpty(configured) && Path.IsPathRooted(configured))
            return configured;

        var baseDir     = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var configDir   = Path.GetDirectoryName(baseDir)!;
        var binDir      = Path.GetDirectoryName(configDir)!;
        var projectDir  = Path.GetDirectoryName(binDir)!;
        var srcDir      = Path.GetDirectoryName(projectDir)!;
        var buildConfig = Path.GetFileName(configDir);
        var tfm         = Path.GetFileName(baseDir);
        var exeName     = "McpServer" + (OperatingSystem.IsWindows() ? ".exe" : "");

        return Path.Combine(srcDir, "McpServer", "bin", buildConfig, tfm, exeName);
    }
}

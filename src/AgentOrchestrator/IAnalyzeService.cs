namespace AgentOrchestrator;

public interface IAnalyzeService
{
    Task<string> AnalyzeAsync(string question, LlmConfig llmConfig, CancellationToken ct = default);
}

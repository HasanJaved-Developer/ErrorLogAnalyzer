using AgentOrchestrator;
using Microsoft.AspNetCore.Mvc;

namespace ErrorAnalyserApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyzeController(IAnalyzeService analyzeService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AnalyzeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        var provider = string.Equals(request.Provider, "Groq", StringComparison.OrdinalIgnoreCase)
            ? LlmProvider.Groq
            : LlmProvider.SelfHosted;

        if (provider == LlmProvider.Groq && string.IsNullOrWhiteSpace(request.ApiKey))
            return BadRequest("An API key is required when using the Groq provider.");

        var llmConfig = new LlmConfig(provider, request.ApiKey);
        var answer = await analyzeService.AnalyzeAsync(request.Question, llmConfig, ct);
        return Ok(new AnalyzeResponse(answer));
    }
}

public record AnalyzeRequest(string Question, string Provider = "SelfHosted", string? ApiKey = null);
public record AnalyzeResponse(string Answer);

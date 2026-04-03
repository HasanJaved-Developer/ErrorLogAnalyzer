using Microsoft.AspNetCore.Mvc;

namespace ErrorAnalyserApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyzeController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AnalyzeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Analyze([FromBody] AnalyzeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("Question is required.");

        // TODO: wire up AgentOrchestrator
        return Ok(new AnalyzeResponse("Agent not yet wired up. Question received: " + request.Question));
    }
}

public record AnalyzeRequest(string Question);
public record AnalyzeResponse(string Answer);

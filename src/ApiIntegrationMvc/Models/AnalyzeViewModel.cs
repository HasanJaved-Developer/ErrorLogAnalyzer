namespace ApiIntegrationMvc.Models;

public class AnalyzeViewModel
{
    public string? Question { get; set; }
    public string? Answer { get; set; }
    public string? Error { get; set; }
    public bool HasResult => Answer is not null;
}

public record AnalyzeResponse(string Answer);

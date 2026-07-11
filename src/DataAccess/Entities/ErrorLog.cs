namespace DataAccess.Entities;

public class ErrorLog
{
    public int Id { get; set; }
    public string ErrorType { get; set; } = "";
    public string Message { get; set; } = "";
    public string StackTrace { get; set; } = "";
    public string Environment { get; set; } = "";
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

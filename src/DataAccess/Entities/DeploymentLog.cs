namespace DataAccess.Entities;

public class DeploymentLog
{
    public int Id { get; set; }
    public string Environment { get; set; } = "";
    public string Version { get; set; } = "";
    public string CommitHash { get; set; } = "";
    public DateTime DeployedAt { get; set; }
    public string FilesChanged { get; set; } = "[]";
}

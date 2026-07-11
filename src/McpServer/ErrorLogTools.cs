using System.ComponentModel;
using System.Text.Json;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;

namespace McpServer;

[McpServerToolType]
public class ErrorLogTools(IDbContextFactory<AppDbContext> dbFactory)
{
    [McpServerTool(Name = "get_error_logs")]
    [Description("""
        Get error logs.

        Parameters:
        - from: start date (yyyy-MM-dd)
        - to: end date (yyyy-MM-dd)
        - errorType: error type ONLY (e.g. NullReferenceException, TimeoutException)

        """)]
    public async Task<string> GetErrorLogs(string? from = null, string? to = null, string? errorType = null)
    {        
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            Console.Error.WriteLine($"EF Conn: {db.Database.GetConnectionString()}");
            

            var query = db.ErrorLogs.AsQueryable();

            if (DateTime.TryParse(from, out var fromDate))
                query = query.Where(e => e.OccurredAt >= fromDate);

            if (DateTime.TryParse(to, out var toDate))
                query = query.Where(e => e.OccurredAt <= toDate.AddDays(1));

            if (!string.IsNullOrWhiteSpace(errorType))
                query = query.Where(e => e.ErrorType == errorType);

            var results = await query
            .GroupBy(e => new { e.ErrorType, e.StackTrace })
            .Select(g => new
            {
                errorType = g.Key.ErrorType,
                count = g.Count(),
                latestStackTrace = g.Key.StackTrace,
                latestOccurredAt = g.Max(x => x.OccurredAt)
            })
            .OrderByDescending(x => x.latestOccurredAt)
            .ToListAsync();

            return JsonSerializer.Serialize(results);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = ex.GetType().Name,
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

    [McpServerTool(Name = "get_recent_deployments")]
    [Description("Get recent deployments. Params: env (optional), date (yyyy-MM-dd optional). Returns changed files.")]
    public async Task<string> GetRecentDeployments(string? env = null, string? date = null)
    {
        try
        {
            var target = DateTime.TryParse(date, out var parsedDate) ? parsedDate : DateTime.UtcNow;
            var environment = string.IsNullOrWhiteSpace(env) ? "production" : env;

            await using var db = await dbFactory.CreateDbContextAsync();
            var deployments = await db.DeploymentLogs
                .Where(d => d.Environment == environment)
                .ToListAsync();

            var results = deployments
                .OrderBy(d => Math.Abs((d.DeployedAt - target).Ticks))
                .Take(2)
                .Select(d => new
                {
                    d.Id,
                    d.Version,
                    d.CommitHash,
                    d.Environment,
                    deployedAt   = d.DeployedAt,
                    filesChanged = JsonSerializer.Deserialize<string[]>(d.FilesChanged)
                });

            return JsonSerializer.Serialize(results);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new
            {
                error = ex.GetType().Name,
                message = ex.Message,
                details = ex.InnerException?.Message
            });
        }
    }

}

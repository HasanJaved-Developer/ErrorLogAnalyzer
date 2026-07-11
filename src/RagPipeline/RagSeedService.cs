using System.Text.Json;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace RagPipeline;

public class RagSeedService(
    IServiceProvider services,
    IConfiguration config,
    ILogger<RagSeedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ragService  = scope.ServiceProvider.GetRequiredService<IRagService>();

        var allFilesChanged = await db.DeploymentLogs
            .Select(d => d.FilesChanged)
            .ToListAsync(ct);

        var distinctPaths = allFilesChanged
            .SelectMany(json => JsonSerializer.Deserialize<string[]>(json) ?? [])
            .Distinct()
            .ToList();

        var basePath = config["RagPipeline:SeedFilesBasePath"];
        if (string.IsNullOrWhiteSpace(basePath))
            basePath = Directory.GetCurrentDirectory();

        var absolutePaths = distinctPaths
            .Select(p => Path.Combine(basePath, p))
            .ToList();

        logger.LogInformation("RagSeedService: indexing {Count} file(s) into Qdrant...", absolutePaths.Count);
        await ragService.IndexFilesAsync(absolutePaths, ct);
        logger.LogInformation("RagSeedService: indexing complete.");
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

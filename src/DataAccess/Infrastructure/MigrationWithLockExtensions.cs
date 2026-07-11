using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataAccess.Infrastructure;

public static class MigrationWithLockExtensions
{
    public static async Task MigrateAndSeedWithSqlLockAsync<TContext>(
        this IHost app,
        string connectionStringName,
        string globalLockName,
        Func<IServiceProvider, CancellationToken, Task> seedAsync,
        CancellationToken ct = default)
        where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var cfg    = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<TContext>>();
        var db     = services.GetRequiredService<TContext>();

        var csb    = new SqlConnectionStringBuilder(cfg.GetConnectionString(connectionStringName));
        var master = new SqlConnectionStringBuilder(csb.ConnectionString) { InitialCatalog = "master" }.ToString();

        await SqlAppLock.WithExclusiveLockAsync(master, globalLockName, async token =>
        {
            logger.LogInformation("Applying EF migrations for '{Db}'...", csb.InitialCatalog);
            await db.Database.MigrateAsync(token);
            logger.LogInformation("Migrations OK for '{Db}'. Seeding...", csb.InitialCatalog);
            await seedAsync(services, token);
            logger.LogInformation("Seed complete for '{Db}'.", csb.InitialCatalog);
        }, ct: ct);
    }
}

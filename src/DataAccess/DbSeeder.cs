using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct)
    {
        var db = services.GetRequiredService<AppDbContext>();

        if (await db.ErrorLogs.AnyAsync(ct))
            return;

        var now = DateTime.UtcNow;

        var deployments = new List<DeploymentLog>
        {
            new()
            {
                Environment  = "production",
                Version      = "v1.0.0",
                CommitHash   = "a1b2c3d4",
                DeployedAt   = now.AddDays(-35),
                FilesChanged = """["SeedFiles/OrderService.cs"]"""
            },
            new()
            {
                Environment  = "production",
                Version      = "v1.1.0",
                CommitHash   = "e5f6g7h8",
                DeployedAt   = now.AddDays(-2),
                FilesChanged = """["SeedFiles/OrderService.cs","SeedFiles/PaymentService.cs"]"""
            }
        };

        db.DeploymentLogs.AddRange(deployments);
        await db.SaveChangesAsync(ct);

        var errors = new List<ErrorLog>
        {            
            new()
            {
                ErrorType   = "NullReferenceException",
                Message     = "Object reference not set to an instance of an object.",
                StackTrace  = "OrderService.cs:34 -> OrderController.cs:22",
                Environment = "production",
                OccurredAt  = now.AddDays(-35).AddHours(6),
                CreatedAt   = now.AddDays(-35).AddHours(6)
            },
            // NullReferenceException flood starts just after v1.1.0 deploy
            new()
            {
                ErrorType   = "NullReferenceException",
                Message     = "Object reference not set to an instance of an object.",
                StackTrace  = "OrderService.cs:42 -> OrderController.cs:22",
                Environment = "production",
                OccurredAt  = now.AddDays(-2).AddHours(1),
                CreatedAt   = now.AddDays(-2).AddHours(1)
            },
            new()
            {
                ErrorType   = "NullReferenceException",
                Message     = "Object reference not set to an instance of an object.",
                StackTrace  = "OrderService.cs:42 -> OrderController.cs:22",
                Environment = "production",
                OccurredAt  = now.AddDays(-2).AddHours(4),
                CreatedAt   = now.AddDays(-2).AddHours(4)
            },
            new()
            {
                ErrorType   = "NullReferenceException",
                Message     = "Object reference not set to an instance of an object.",
                StackTrace  = "OrderService.cs:42 -> OrderController.cs:22",
                Environment = "production",
                OccurredAt  = now.AddDays(-2).AddHours(6),
                CreatedAt   = now.AddDays(-2).AddHours(6)
            },
            // TimeoutException in PaymentService
            new()
            {
                ErrorType   = "TimeoutException",
                Message     = "The operation has timed out.",
                StackTrace  = "PaymentService.cs:28 -> PaymentController.cs:18",
                Environment = "production",
                OccurredAt  = now.AddDays(-2).AddHours(2),
                CreatedAt   = now.AddDays(-2).AddHours(2)
            }
        };

        db.ErrorLogs.AddRange(errors);
        await db.SaveChangesAsync(ct);
    }
}

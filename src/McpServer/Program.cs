using DataAccess;
using McpServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.Logging.ClearProviders();

Console.Error.WriteLine($"BaseDir: {AppContext.BaseDirectory}");
Console.Error.WriteLine($"Conn: {builder.Configuration.GetConnectionString("Default")}");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")!,
        sql => sql.EnableRetryOnFailure()
    ));

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ErrorLogTools>();

await builder.Build().RunAsync();

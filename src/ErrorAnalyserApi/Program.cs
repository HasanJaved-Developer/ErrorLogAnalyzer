using AgentOrchestrator;
using DataAccess;
using DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.AI;
using OpenAI;
using RagPipeline;
using System.ClientModel;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Error Log Analyzer API",
        Version = "v1",
        Description = "Agentic AI system that reasons over production error logs, deployment history, and source code changes."
    });
});

var ollamaEndpoint = builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434/v1";
var ollamaModel    = builder.Configuration["Ollama:Model"]    ?? "qwen2.5:3b";
var groqEndpoint   = builder.Configuration["Groq:Endpoint"]   ?? "https://api.groq.com/openai/v1";
var groqModel      = builder.Configuration["Groq:Model"]      ?? "llama-3.3-70b-versatile";

builder.Services.AddSingleton<Func<LlmConfig, IChatClient>>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    var ollamaClient = new OpenAIClient(
            new ApiKeyCredential("not-required"),
            new OpenAIClientOptions { Endpoint = new Uri(ollamaEndpoint), NetworkTimeout = TimeSpan.FromMinutes(10) })
        .GetChatClient(ollamaModel)
        .AsIChatClient()
        .AsBuilder().UseLogging(loggerFactory).Build();

    return llmConfig =>
    {
        if (llmConfig.Provider == LlmProvider.Groq)
            return new OpenAIClient(
                    new ApiKeyCredential(llmConfig.ApiKey!),
                    new OpenAIClientOptions { Endpoint = new Uri(groqEndpoint) })
                .GetChatClient(groqModel)
                .AsIChatClient()
                .AsBuilder().UseLogging(loggerFactory).Build();

        return ollamaClient;
    };
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<OllamaEmbedder>(sp =>
{
    var client   = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var endpoint = builder.Configuration["Ollama:Endpoint"] ?? "http://localhost:11434/v1";
    var logger   = sp.GetRequiredService<ILogger<OllamaEmbedder>>();
    return new OllamaEmbedder(client, endpoint, logger);
});
builder.Services.AddSingleton<QdrantStore>();
builder.Services.AddScoped<IRagService, RagService>();
builder.Services.AddHostedService<RagSeedService>();
builder.Services.AddScoped<IAnalyzeService, AgentAnalyzeService>();

var app = builder.Build();

await app.MigrateAndSeedWithSqlLockAsync<AppDbContext>(
    connectionStringName: "Default",
    globalLockName: "ELA_GLOBAL_MIGRATE_SEED",
    seedAsync: DbSeeder.SeedAsync);

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Error Log Analyzer API v1");
    options.RoutePrefix = string.Empty;
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

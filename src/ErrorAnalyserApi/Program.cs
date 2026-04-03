using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Error Log Analyzer API v1");
    options.RoutePrefix = string.Empty; // Swagger UI at root
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

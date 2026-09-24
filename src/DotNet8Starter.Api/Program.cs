var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    application = "DotNet8Starter.Api",
    version = "1.0.0",
    message = "Hello from .NET 8 on Azure App Service",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/api/info", () => Results.Ok(new
{
    runtime = Environment.Version.ToString(),
    environment = app.Environment.EnvironmentName,
    machine = Environment.MachineName,
    timestampUtc = DateTime.UtcNow
}));

app.MapHealthChecks("/health");

app.Run();

public partial class Program { }

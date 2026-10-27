using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PathLens.Application;
using PathLens.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Structured (JSON) console logging so logs stay machine-readable.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var frontendOrigin = builder.Configuration["Cors:FrontendOrigin"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(frontendOrigin).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Consistent RFC 9457 ProblemDetails for unhandled errors; no stack traces leak.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Correlation ID: honour an incoming one, otherwise generate it; echo it back.
app.Use(async (context, next) =>
{
    const string header = "X-Correlation-Id";
    var correlationId = context.Request.Headers[header].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = Guid.NewGuid().ToString("n");
    }

    context.Items[header] = correlationId;
    context.Response.Headers[header] = correlationId;

    using (app.Logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
    {
        await next(context);
    }
});

app.UseCors("frontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

static Task WriteHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var payload = JsonSerializer.Serialize(new
    {
        status = report.Status.ToString(),
        checks = report.Entries.Select(entry => new
        {
            name = entry.Key,
            status = entry.Value.Status.ToString(),
            description = entry.Value.Description,
        }),
    });
    return context.Response.WriteAsync(payload);
}

// Liveness: the process itself is up. No dependency checks run here.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = WriteHealthResponse,
});

// Readiness: dependencies (PostgreSQL) must be reachable; otherwise 503.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = WriteHealthResponse,
});

app.Run();

public partial class Program;

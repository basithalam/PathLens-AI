using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using PathLens.Api.Endpoints;
using PathLens.Application;
using PathLens.Infrastructure;
using PathLens.Infrastructure.Persistence;

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

// Apply pending migrations at startup. If the database is unreachable the API
// still starts so /health/live works and /health/ready can report the failure.
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PathLensDbContext>();
    await dbContext.Database.MigrateAsync();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    app.Logger.LogWarning(ex, "Database migration at startup failed; health/ready will report database status.");
}

app.MapReportEndpoints();
app.MapEvidenceEndpoints();
app.MapComparisonEndpoints();
app.MapMapEndpoints();
app.MapRouteEndpoints();

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

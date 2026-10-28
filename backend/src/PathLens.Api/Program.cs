using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PathLens.Api;
using PathLens.Api.Endpoints;
using PathLens.Application;
using PathLens.Application.Users;
using PathLens.Infrastructure;
using PathLens.Infrastructure.Auth;
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
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? "LOCAL_DEV_ONLY_CHANGE_ME_PATHLENS_SIGNING_KEY_32CHARS";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PathLens",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PathLensWeb",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthEndpoints.ReviewerPolicy, policy =>
        policy.RequireRole("Reviewer", "Admin"));

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
app.UseAuthentication();
app.UseAuthorization();

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

// Seed the development reviewer/admin/user accounts once (hashed passwords).
try
{
    using var seedScope = app.Services.CreateScope();
    var userRepository = seedScope.ServiceProvider.GetRequiredService<IUserRepository>();
    await SeedUsers.SeedAsync(userRepository, CancellationToken.None);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    app.Logger.LogWarning(ex, "Seed user creation failed; login will be unavailable until the database is reachable.");
}

app.MapAuthEndpoints();
app.MapReviewEndpoints();
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

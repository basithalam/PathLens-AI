using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace PathLens.Infrastructure.Health;

/// <summary>
/// Reports whether PostgreSQL is reachable with the configured connection
/// string. Never throws: an unreachable database is Unhealthy, not a crash.
/// </summary>
public sealed class DatabaseHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("Database connection string is not configured.");
        }

        try
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Timeout = 3,
                CommandTimeout = 3,
            };

            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (Exception ex) when (ex is NpgsqlException or InvalidOperationException or TimeoutException)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is not reachable.", ex);
        }
    }
}

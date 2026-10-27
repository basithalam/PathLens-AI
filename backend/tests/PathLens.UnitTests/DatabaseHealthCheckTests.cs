using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PathLens.Infrastructure.Health;

namespace PathLens.UnitTests;

public class DatabaseHealthCheckTests
{
    [Fact]
    public async Task Missing_connection_string_reports_unhealthy_without_throwing()
    {
        var configuration = new ConfigurationBuilder().Build();
        var check = new DatabaseHealthCheck(configuration);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task Unreachable_database_reports_unhealthy_without_throwing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Port=5432;Database=pathlens_does_not_exist;Username=postgres;Password=invalid;Timeout=2;Command Timeout=2",
            })
            .Build();
        var check = new DatabaseHealthCheck(configuration);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}

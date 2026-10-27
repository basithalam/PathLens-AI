using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PathLens.IntegrationTests;

public class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
            builder.UseSetting(
                "ConnectionStrings:DefaultConnection",
                "Host=localhost;Port=5432;Database=pathlens_does_not_exist;Username=postgres;Password=invalid;Timeout=2;Command Timeout=2"))
            .CreateClient();
    }

    [Fact]
    public async Task Live_returns_200_even_when_database_is_unreachable()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", body);
    }

    [Fact]
    public async Task Ready_returns_503_when_database_is_unreachable()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("database", body);
        Assert.Contains("Unhealthy", body);
    }

    [Fact]
    public async Task Responses_include_correlation_id_header()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }
}

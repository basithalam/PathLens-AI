using System.Net.Http;
using Microsoft.Extensions.Options;
using PathLens.Application.Ai;
using PathLens.Infrastructure.Ai;

namespace PathLens.UnitTests;

public class MetaAnalysisServiceTests
{
    [Fact]
    public async Task Unconfigured_meta_provider_fails_loudly_instead_of_faking_a_result()
    {
        var service = new MetaImageAnalysisService(
            new UnusedHttpClientFactory(),
            Options.Create(new MetaAiOptions()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AnalyzeAsync(new ImageAnalysisRequest([1, 2, 3], "image/png"), CancellationToken.None));

        Assert.Contains("not configured", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class UnusedHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}

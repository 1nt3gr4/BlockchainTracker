using System.Net;
using System.Text.Json;

namespace BlockchainTracker.FunctionalTests.Endpoints;

public class SwaggerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SwaggerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SwaggerJson_ReturnsValidOpenApiSpec()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        Assert.True(doc.RootElement.TryGetProperty("openapi", out var version));
        Assert.StartsWith("3.", version.GetString());

        Assert.True(doc.RootElement.TryGetProperty("paths", out var paths));
        Assert.True(paths.TryGetProperty("/api/chains", out _));
        Assert.True(paths.TryGetProperty("/api/chains/tracked", out _));
        Assert.True(paths.TryGetProperty("/api/chains/{chainName}/latest", out _));
        Assert.True(paths.TryGetProperty("/api/chains/{chainName}/history", out _));
    }

    [Fact]
    public async Task SwaggerUi_ReturnsHtml()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

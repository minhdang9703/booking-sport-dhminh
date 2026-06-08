using System.Net;
using System.Text.Json;
using BookingSport.Api.Tests.Integration.Support;
using FluentAssertions;

namespace BookingSport.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class GlobalExceptionMiddlewareIntegrationTests(IntegrationTestFactory factory)
{
    [SkippableFact]
    public async Task GlobalExceptionMiddleware_WhenUnhandledExceptionOccurs_ReturnsJsonError()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/test/throw");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(500);
        root.GetProperty("title").GetString().Should().Be("Unexpected error");
        root.GetProperty("message").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        root.GetProperty("timestamp").GetString().Should().NotBeNullOrWhiteSpace();
        content.Should().NotContain(" at ");
        content.Should().NotContain(nameof(InvalidOperationException));
    }
}

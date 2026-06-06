using System.Net;
using BookingSport.Api.DTOs.Courts;
using BookingSport.Api.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookingSport.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class CourtSearchIntegrationTests(IntegrationTestFactory factory)
{
    [SkippableFact]
    public async Task GetCourts_WithKeywordAndCourtType_UsesPostgresCaseInsensitiveSearch()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        await factory.SeedAsync(dbContext =>
        {
            dbContext.Courts.AddRange(
                IntegrationSeedData.Court("Alpha Arena", "Badminton"),
                IntegrationSeedData.Court("Beta Yard", "Tennis"),
                IntegrationSeedData.Court("Deleted Alpha", "Badminton", deletedAt: DateTimeOffset.UtcNow));
            return Task.CompletedTask;
        });
        var client = CreateClient();

        var keywordResponse = await client.GetAsync("/api/courts?keyword=alpha");
        keywordResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var keywordResults = await keywordResponse.ReadJsonAsync<IReadOnlyList<CourtResponse>>();
        keywordResults.Should().ContainSingle(court => court.Name == "Alpha Arena");

        var typeResponse = await client.GetAsync("/api/courts?courtType=BADM");
        typeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var typeResults = await typeResponse.ReadJsonAsync<IReadOnlyList<CourtResponse>>();
        typeResults.Should().ContainSingle(court => court.Name == "Alpha Arena");
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}

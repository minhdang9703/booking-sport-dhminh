using System.Net;
using System.Net.Http.Json;
using BookingSport.Api.DTOs.Courts;
using BookingSport.Api.DTOs.PriceRules;
using BookingSport.Api.Enums;
using BookingSport.Api.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookingSport.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class ApiErrorMappingTests(IntegrationTestFactory factory)
{
    [SkippableFact]
    public async Task Availability_WhenCourtDoesNotExist_ReturnsNotFound()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        var client = CreateClient();

        var response = await client.GetAsync(
            $"/api/courts/{Guid.NewGuid()}/available-schedules?date=2026-06-08");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [SkippableFact]
    public async Task Dashboard_WhenDateRangeIsInvalid_ReturnsBadRequest()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        await factory.SeedAsync(dbContext =>
        {
            IntegrationSeedData.AddCoreData(dbContext);
            return Task.CompletedTask;
        });
        var client = CreateClient();
        var login = await client.LoginAsync("admin.integration@test.local");
        client.SetBearerToken(login.AccessToken);

        var response = await client.GetAsync("/api/dashboard/revenue?fromDate=2026-06-10&toDate=2026-06-08");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.ReadErrorAsync()).Message.Should().Be("FromDate must be less than or equal to ToDate.");
    }

    [SkippableFact]
    public async Task AdminCreateEndpoints_WhenBusinessRulesConflict_ReturnConflict()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        await factory.SeedAsync(dbContext =>
        {
            IntegrationSeedData.AddCoreData(dbContext);
            return Task.CompletedTask;
        });
        var client = CreateClient();
        var login = await client.LoginAsync("admin.integration@test.local");
        client.SetBearerToken(login.AccessToken);

        var duplicateCourtResponse = await client.PostAsJsonAsync("/api/courts", new CourtCreateRequest
        {
            Name = "Integration Court Alpha",
            CourtType = "Badminton",
            Status = CourtStatus.Active
        });
        duplicateCourtResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var overlapRuleResponse = await client.PostAsJsonAsync("/api/price-rules", new PriceRuleCreateRequest
        {
            Name = "Overlap Monday",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(10, 0),
            HourlyPrice = 130_000m,
            IsEnabled = true
        });
        overlapRuleResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}

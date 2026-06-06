using System.Net;
using System.Net.Http.Json;
using BookingSport.Api.DTOs.Bookings;
using BookingSport.Api.DTOs.Courts;
using BookingSport.Api.DTOs.Dashboard;
using BookingSport.Api.DTOs.PriceRules;
using BookingSport.Api.Enums;
using BookingSport.Api.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookingSport.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class AdminManagementFlowTests(IntegrationTestFactory factory)
{
    [SkippableFact]
    public async Task AdminManagementFlow_ManagesCourtsPriceRulesBookingsAndRevenue()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        SeedIds seedIds = default!;
        Guid bookingId = Guid.Empty;
        await factory.SeedAsync(dbContext =>
        {
            seedIds = IntegrationSeedData.AddCoreData(dbContext);
            var booking = IntegrationSeedData.Booking(
                seedIds.CustomerId,
                seedIds.CourtId,
                IntegrationSeedData.BookingDate,
                new TimeOnly(9, 0),
                new TimeOnly(10, 0),
                BookingStatus.Completed,
                totalPrice: 120_000m);
            bookingId = booking.Id;
            dbContext.Bookings.Add(booking);
            return Task.CompletedTask;
        });
        var customerClient = CreateClient();
        var customerLogin = await customerClient.LoginAsync("customer.integration@test.local");
        customerClient.SetBearerToken(customerLogin.AccessToken);
        var forbidden = await customerClient.GetAsync("/api/bookings");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var adminClient = CreateClient();
        var adminLogin = await adminClient.LoginAsync("admin.integration@test.local");
        adminClient.SetBearerToken(adminLogin.AccessToken);

        var createCourtResponse = await adminClient.PostAsJsonAsync("/api/courts", new CourtCreateRequest
        {
            Name = "Integration Admin Court",
            CourtType = "Pickleball",
            Status = CourtStatus.Active
        });
        createCourtResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createPriceRuleResponse = await adminClient.PostAsJsonAsync("/api/price-rules", new PriceRuleCreateRequest
        {
            Name = "Integration Tuesday Evening",
            DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(18, 0),
            EndTime = new TimeOnly(20, 0),
            HourlyPrice = 150_000m,
            IsEnabled = true
        });
        createPriceRuleResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var bookingsResponse = await adminClient.GetAsync("/api/bookings");
        bookingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var bookings = await bookingsResponse.ReadJsonAsync<IReadOnlyList<BookingResponse>>();
        bookings.Should().Contain(booking => booking.Id == bookingId);

        var statusResponse = await adminClient.PutAsJsonAsync($"/api/bookings/{bookingId}/status", new BookingUpdateStatusRequest
        {
            Status = BookingStatus.Completed
        });
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var revenueResponse = await adminClient.GetAsync(
            "/api/dashboard/revenue?fromDate=2026-06-08&toDate=2026-06-08&period=Day");
        revenueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var revenue = await revenueResponse.ReadJsonAsync<RevenueDashboardResponse>();
        revenue.TotalRevenue.Should().Be(120_000m);
        revenue.CompletedBookingCount.Should().Be(1);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}

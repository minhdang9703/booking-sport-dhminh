using System.Net;
using System.Net.Http.Json;
using BookingSport.Api.DTOs.Availability;
using BookingSport.Api.DTOs.Bookings;
using BookingSport.Api.DTOs.Courts;
using BookingSport.Api.Enums;
using BookingSport.Api.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookingSport.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class UserBookingFlowTests(IntegrationTestFactory factory)
{
    [SkippableFact]
    public async Task CustomerBookingFlow_CreatesBookingAndBlocksOverlappingSlot()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        SeedIds seedIds = default!;
        await factory.SeedAsync(dbContext =>
        {
            seedIds = IntegrationSeedData.AddCoreData(dbContext);
            return Task.CompletedTask;
        });
        var client = CreateClient();
        var login = await client.LoginAsync("customer.integration@test.local");
        client.SetBearerToken(login.AccessToken);

        var courtsResponse = await client.GetAsync("/api/courts");
        courtsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var courts = await courtsResponse.ReadJsonAsync<IReadOnlyList<CourtResponse>>();
        courts.Should().Contain(court => court.Id == seedIds.CourtId);

        var availabilityResponse = await client.GetAsync(
            $"/api/courts/{seedIds.CourtId}/available-schedules?date={IntegrationSeedData.BookingDate:yyyy-MM-dd}");
        availabilityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var schedules = await availabilityResponse.ReadJsonAsync<IReadOnlyList<AvailableScheduleResponse>>();
        schedules.Should().ContainSingle(schedule =>
            schedule.StartTime == new TimeOnly(8, 0) &&
            schedule.EndTime == new TimeOnly(12, 0));

        var created = await client.CreateBookingAsync(new BookingCreateRequest
        {
            CourtId = seedIds.CourtId,
            BookingDate = IntegrationSeedData.BookingDate,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(9, 0),
            PaymentType = PaymentType.Cash
        });
        created.Status.Should().Be(BookingStatus.Pending);
        created.TotalPrice.Should().Be(120_000m);

        var myBookingsResponse = await client.GetAsync("/api/bookings/my");
        myBookingsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var myBookings = await myBookingsResponse.ReadJsonAsync<IReadOnlyList<BookingResponse>>();
        myBookings.Should().ContainSingle(booking => booking.Id == created.Id);

        var overlapResponse = await client.PostAsJsonAsync("/api/bookings", new BookingCreateRequest
        {
            CourtId = seedIds.CourtId,
            BookingDate = IntegrationSeedData.BookingDate,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(9, 30),
            PaymentType = PaymentType.Cash
        });

        overlapResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}

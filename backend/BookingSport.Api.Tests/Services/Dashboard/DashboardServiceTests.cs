using BookingSport.Api.DTOs.Dashboard;
using BookingSport.Api.Enums;
using BookingSport.Api.Services.Dashboard;
using BookingSport.Api.Tests.TestSupport;
using FluentAssertions;

namespace BookingSport.Api.Tests.Services.Dashboard;

public class DashboardServiceTests
{
    [Fact]
    public async Task GetRevenueDashboardAsync_WhenFromDateAfterToDate_ReturnsBadRequest()
    {
        await using var db = await TestDb.CreateAsync();
        var service = new DashboardService(db.Context);

        var result = await service.GetRevenueDashboardAsync(new RevenueDashboardQuery
        {
            FromDate = new DateOnly(2026, 6, 10),
            ToDate = new DateOnly(2026, 6, 8)
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("FromDate must be less than or equal to ToDate.");
    }

    [Fact]
    public async Task GetRevenueDashboardAsync_CalculatesCompletedRevenueAndDailySeries()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.Bookings.AddRange(
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: new DateOnly(2026, 6, 8), status: BookingStatus.Completed, totalPrice: 100_000m),
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: new DateOnly(2026, 6, 8), startTime: new TimeOnly(10, 0), endTime: new TimeOnly(11, 0), status: BookingStatus.Completed, totalPrice: 200_000m),
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: new DateOnly(2026, 6, 9), status: BookingStatus.Pending, totalPrice: 999_000m),
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: new DateOnly(2026, 6, 10), status: BookingStatus.Completed, totalPrice: 300_000m, deletedAt: DateTimeOffset.UtcNow));
        await db.Context.SaveChangesAsync();
        var service = new DashboardService(db.Context);

        var result = await service.GetRevenueDashboardAsync(new RevenueDashboardQuery
        {
            FromDate = new DateOnly(2026, 6, 8),
            ToDate = new DateOnly(2026, 6, 10),
            Period = RevenuePeriod.Day
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.TotalRevenue.Should().Be(300_000m);
        result.Value.CompletedBookingCount.Should().Be(2);
        result.Value.AverageBookingValue.Should().Be(150_000m);
        result.Value.DailyRevenue.Should().HaveCount(3);
        result.Value.DailyRevenue.Select(point => point.Revenue).Should().Equal(300_000m, 0m, 0m);
        result.Value.RevenuePoints.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetRevenueDashboardAsync_WithWeekPeriod_GroupsByMondayWeeks()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.Bookings.AddRange(
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: new DateOnly(2026, 6, 8), status: BookingStatus.Completed, totalPrice: 100_000m),
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: new DateOnly(2026, 6, 15), status: BookingStatus.Completed, totalPrice: 200_000m));
        await db.Context.SaveChangesAsync();
        var service = new DashboardService(db.Context);

        var result = await service.GetRevenueDashboardAsync(new RevenueDashboardQuery
        {
            FromDate = new DateOnly(2026, 6, 8),
            ToDate = new DateOnly(2026, 6, 15),
            Period = RevenuePeriod.Week
        }, CancellationToken.None);

        result.Value!.RevenuePoints.Should().HaveCount(2);
        result.Value.RevenuePoints.Select(point => point.Revenue).Should().Equal(100_000m, 200_000m);
        result.Value.RevenuePoints[0].FromDate.Should().Be(new DateOnly(2026, 6, 8));
        result.Value.RevenuePoints[0].ToDate.Should().Be(new DateOnly(2026, 6, 14));
    }

    [Fact]
    public async Task GetRevenueDashboardAsync_WithMonthPeriod_GroupsByMonthAndClipsBoundaries()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.Bookings.AddRange(
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: new DateOnly(2026, 6, 30), status: BookingStatus.Completed, totalPrice: 100_000m),
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: new DateOnly(2026, 7, 1), status: BookingStatus.Completed, totalPrice: 200_000m));
        await db.Context.SaveChangesAsync();
        var service = new DashboardService(db.Context);

        var result = await service.GetRevenueDashboardAsync(new RevenueDashboardQuery
        {
            FromDate = new DateOnly(2026, 6, 15),
            ToDate = new DateOnly(2026, 7, 15),
            Period = RevenuePeriod.Month
        }, CancellationToken.None);

        result.Value!.RevenuePoints.Should().HaveCount(2);
        result.Value.RevenuePoints.Select(point => point.Label).Should().Equal("2026-06", "2026-07");
        result.Value.RevenuePoints.Select(point => point.Revenue).Should().Equal(100_000m, 200_000m);
        result.Value.RevenuePoints[0].FromDate.Should().Be(new DateOnly(2026, 6, 15));
        result.Value.RevenuePoints[1].ToDate.Should().Be(new DateOnly(2026, 7, 15));
    }
}

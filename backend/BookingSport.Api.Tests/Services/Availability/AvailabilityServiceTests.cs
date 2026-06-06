using BookingSport.Api.Enums;
using BookingSport.Api.Services.Availability;
using BookingSport.Api.Tests.TestSupport;
using FluentAssertions;

namespace BookingSport.Api.Tests.Services.Availability;

public class AvailabilityServiceTests
{
    private static readonly DateOnly Monday = new(2026, 6, 8);

    [Fact]
    public async Task GetAvailableSchedulesAsync_WhenCourtDoesNotExist_ReturnsNotFound()
    {
        await using var db = await TestDb.CreateAsync();
        var service = new AvailabilityService(db.Context);

        var result = await service.GetAvailableSchedulesAsync(Guid.NewGuid(), Monday, CancellationToken.None);

        result.Found.Should().BeFalse();
        result.Schedules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSchedulesAsync_WhenCourtIsInactive_ReturnsEmptySchedules()
    {
        await using var db = await TestDb.CreateAsync();
        var court = TestData.Court(status: CourtStatus.Inactive);
        db.Context.Courts.Add(court);
        await db.Context.SaveChangesAsync();
        var service = new AvailabilityService(db.Context);

        var result = await service.GetAvailableSchedulesAsync(court.Id, Monday, CancellationToken.None);

        result.Found.Should().BeTrue();
        result.Schedules.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAvailableSchedulesAsync_WhenNoBookings_ReturnsFullRuleRange()
    {
        await using var db = await TestDb.CreateAsync();
        var court = TestData.Court();
        var rule = TestData.PriceRule(dayOfWeek: Monday.DayOfWeek, startTime: new TimeOnly(8, 0), endTime: new TimeOnly(10, 0));
        db.Context.Courts.Add(court);
        db.Context.PriceRules.Add(rule);
        await db.Context.SaveChangesAsync();
        var service = new AvailabilityService(db.Context);

        var result = await service.GetAvailableSchedulesAsync(court.Id, Monday, CancellationToken.None);

        result.Schedules.Should().ContainSingle();
        result.Schedules[0].StartTime.Should().Be(new TimeOnly(8, 0));
        result.Schedules[0].EndTime.Should().Be(new TimeOnly(10, 0));
        result.Schedules[0].HourlyPrice.Should().Be(rule.HourlyPrice);
    }

    [Fact]
    public async Task GetAvailableSchedulesAsync_WhenBlockingBookingExists_SplitsAvailableRange()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.PriceRules.Add(TestData.PriceRule(dayOfWeek: Monday.DayOfWeek, startTime: new TimeOnly(8, 0), endTime: new TimeOnly(12, 0)));
        db.Context.Bookings.Add(TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday, startTime: new TimeOnly(9, 0), endTime: new TimeOnly(10, 0), status: BookingStatus.Confirmed));
        await db.Context.SaveChangesAsync();
        var service = new AvailabilityService(db.Context);

        var result = await service.GetAvailableSchedulesAsync(court.Id, Monday, CancellationToken.None);

        result.Schedules.Select(schedule => (schedule.StartTime, schedule.EndTime))
            .Should()
            .Equal(
                (new TimeOnly(8, 0), new TimeOnly(9, 0)),
                (new TimeOnly(10, 0), new TimeOnly(12, 0)));
    }

    [Fact]
    public async Task GetAvailableSchedulesAsync_IgnoresCompletedAndCancelledBookings()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.PriceRules.Add(TestData.PriceRule(dayOfWeek: Monday.DayOfWeek, startTime: new TimeOnly(8, 0), endTime: new TimeOnly(10, 0)));
        db.Context.Bookings.AddRange(
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday, startTime: new TimeOnly(8, 0), endTime: new TimeOnly(9, 0), status: BookingStatus.Completed),
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday, startTime: new TimeOnly(9, 0), endTime: new TimeOnly(10, 0), status: BookingStatus.Cancelled));
        await db.Context.SaveChangesAsync();
        var service = new AvailabilityService(db.Context);

        var result = await service.GetAvailableSchedulesAsync(court.Id, Monday, CancellationToken.None);

        result.Schedules.Should().ContainSingle();
        result.Schedules[0].StartTime.Should().Be(new TimeOnly(8, 0));
        result.Schedules[0].EndTime.Should().Be(new TimeOnly(10, 0));
    }

    [Fact]
    public async Task GetAvailableSchedulesAsync_WithMultipleRules_ReturnsSchedulesSorted()
    {
        await using var db = await TestDb.CreateAsync();
        var court = TestData.Court();
        db.Context.Courts.Add(court);
        db.Context.PriceRules.AddRange(
            TestData.PriceRule(name: "Late", dayOfWeek: Monday.DayOfWeek, startTime: new TimeOnly(14, 0), endTime: new TimeOnly(16, 0)),
            TestData.PriceRule(name: "Early", dayOfWeek: Monday.DayOfWeek, startTime: new TimeOnly(8, 0), endTime: new TimeOnly(10, 0)));
        await db.Context.SaveChangesAsync();
        var service = new AvailabilityService(db.Context);

        var result = await service.GetAvailableSchedulesAsync(court.Id, Monday, CancellationToken.None);

        result.Schedules.Select(schedule => schedule.StartTime)
            .Should()
            .Equal(new TimeOnly(8, 0), new TimeOnly(14, 0));
    }
}

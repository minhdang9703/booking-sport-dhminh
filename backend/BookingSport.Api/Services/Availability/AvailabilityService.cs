using BookingSport.Api.Data;
using BookingSport.Api.DTOs.Availability;
using BookingSport.Api.Entities;
using BookingSport.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Services.Availability;

public class AvailabilityService(AppDbContext dbContext) : IAvailabilityService
{
    private static readonly BookingStatus[] BlockingStatuses =
    [
        BookingStatus.Pending,
        BookingStatus.Confirmed
    ];

    public async Task<AvailabilityResult> GetAvailableSchedulesAsync(
        Guid courtId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var court = await dbContext.Courts
            .AsNoTracking()
            .FirstOrDefaultAsync(court =>
                court.Id == courtId &&
                court.DeletedAt == null,
                cancellationToken);

        if (court is null)
        {
            return AvailabilityResult.NotFound();
        }

        if (court.Status != CourtStatus.Active)
        {
            return AvailabilityResult.Success(Array.Empty<AvailableScheduleResponse>());
        }

        var priceRules = await dbContext.PriceRules
            .AsNoTracking()
            .Where(rule =>
                rule.DayOfWeek == date.DayOfWeek &&
                rule.IsEnabled &&
                rule.StartTime < rule.EndTime)
            .OrderBy(rule => rule.StartTime)
            .ThenBy(rule => rule.EndTime)
            .ToListAsync(cancellationToken);

        var bookings = await dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.CourtId == courtId &&
                booking.BookingDate == date &&
                booking.DeletedAt == null &&
                BlockingStatuses.Contains(booking.Status))
            .OrderBy(booking => booking.StartTime)
            .ThenBy(booking => booking.EndTime)
            .ToListAsync(cancellationToken);

        var availableRanges = priceRules
            .SelectMany(rule => BuildAvailableRanges(rule, court, date, bookings))
            .OrderBy(range => range.StartTime)
            .ThenBy(range => range.EndTime)
            .ToList();

        return AvailabilityResult.Success(availableRanges);
    }

    private static IEnumerable<AvailableScheduleResponse> BuildAvailableRanges(
        PriceRule rule,
        Court court,
        DateOnly date,
        IReadOnlyList<Booking> bookings)
    {
        var cursor = rule.StartTime;
        var overlappingBookings = bookings
            .Where(booking => booking.StartTime < rule.EndTime && booking.EndTime > rule.StartTime)
            .OrderBy(booking => booking.StartTime)
            .ThenBy(booking => booking.EndTime);

        foreach (var booking in overlappingBookings)
        {
            var bookedStart = MaxTime(booking.StartTime, rule.StartTime);
            var bookedEnd = MinTime(booking.EndTime, rule.EndTime);

            if (cursor < bookedStart)
            {
                yield return MapAvailableRange(rule, court, date, cursor, bookedStart);
            }

            if (cursor < bookedEnd)
            {
                cursor = bookedEnd;
            }
        }

        if (cursor < rule.EndTime)
        {
            yield return MapAvailableRange(rule, court, date, cursor, rule.EndTime);
        }
    }

    private static AvailableScheduleResponse MapAvailableRange(
        PriceRule rule,
        Court court,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        return new AvailableScheduleResponse
        {
            PriceRuleId = rule.Id,
            CourtId = court.Id,
            CourtName = court.Name,
            Date = date,
            DayOfWeek = date.DayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            HourlyPrice = rule.HourlyPrice,
            IsAvailable = true
        };
    }

    private static TimeOnly MaxTime(TimeOnly first, TimeOnly second)
    {
        return first > second ? first : second;
    }

    private static TimeOnly MinTime(TimeOnly first, TimeOnly second)
    {
        return first < second ? first : second;
    }
}

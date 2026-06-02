using BookingSport.Api.Data;
using BookingSport.Api.DTOs.Availability;
using BookingSport.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Services.Availability;

public class AvailabilityService(AppDbContext dbContext) : IAvailabilityService
{
    public async Task<AvailabilityResult> GetAvailableSchedulesAsync(
        Guid courtId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var court = await dbContext.Courts
            .AsNoTracking()
            .Include(court => court.Venue)
            .Include(court => court.Sport)
            .FirstOrDefaultAsync(court =>
                court.Id == courtId &&
                court.DeletedAt == null &&
                court.Venue.DeletedAt == null &&
                court.Sport.DeletedAt == null,
                cancellationToken);

        if (court is null)
        {
            return AvailabilityResult.NotFound();
        }

        if (court.Status != CourtStatus.Active)
        {
            return AvailabilityResult.Success(Array.Empty<AvailableScheduleResponse>());
        }

        var blockedStatuses = new[] { BookingStatus.Pending, BookingStatus.Confirmed };
        var bookedScheduleIds = await dbContext.Bookings
            .AsNoTracking()
            .Where(booking =>
                booking.BookingDate == date &&
                booking.DeletedAt == null &&
                blockedStatuses.Contains(booking.Status))
            .Select(booking => booking.CourtScheduleId)
            .ToListAsync(cancellationToken);

        var dayOfWeek = date.DayOfWeek;

        var schedules = await dbContext.CourtSchedules
            .AsNoTracking()
            .Where(schedule =>
                schedule.CourtId == courtId &&
                schedule.DayOfWeek == dayOfWeek &&
                schedule.IsAvailable &&
                schedule.DeletedAt == null &&
                !bookedScheduleIds.Contains(schedule.Id))
            .OrderBy(schedule => schedule.StartTime)
            .ThenBy(schedule => schedule.EndTime)
            .Select(schedule => new AvailableScheduleResponse
            {
                ScheduleId = schedule.Id,
                CourtId = court.Id,
                CourtName = court.Name,
                Date = date,
                DayOfWeek = dayOfWeek,
                StartTime = schedule.StartTime,
                EndTime = schedule.EndTime,
                Price = schedule.Price,
                IsAvailable = schedule.IsAvailable
            })
            .ToListAsync(cancellationToken);

        return AvailabilityResult.Success(schedules);
    }
}

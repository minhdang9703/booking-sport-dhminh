using BookingSport.Api.Data;
using BookingSport.Api.DTOs.Bookings;
using BookingSport.Api.Entities;
using BookingSport.Api.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BookingSport.Api.Services.Bookings;

public class BookingService(AppDbContext dbContext) : IBookingService
{
    private static readonly BookingStatus[] BlockingStatuses =
    [
        BookingStatus.Pending,
        BookingStatus.Confirmed
    ];

    public async Task<BookingResult<BookingResponse>> CreateBookingAsync(
        Guid userId,
        BookingCreateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateCreateRequestAsync(userId, request, cancellationToken);

        if (!validation.Succeeded)
        {
            return validation;
        }

        var schedule = await GetScheduleForBookingAsync(request.CourtScheduleId, cancellationToken);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourtScheduleId = request.CourtScheduleId,
            BookingDate = request.BookingDate,
            Status = BookingStatus.Pending,
            TotalPrice = schedule!.Price,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Bookings.Add(booking);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateBookingException(exception))
        {
            return BookingResult<BookingResponse>.Conflict("This schedule has already been booked.");
        }

        var response = await GetBookingResponseQuery()
            .Where(existingBooking => existingBooking.Id == booking.Id)
            .Select(existingBooking => MapBookingResponse(existingBooking))
            .FirstAsync(cancellationToken);

        return BookingResult<BookingResponse>.Success(response);
    }

    public async Task<IReadOnlyList<BookingResponse>> GetMyBookingsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await GetBookingResponseQuery()
            .Where(booking => booking.UserId == userId)
            .OrderByDescending(booking => booking.BookingDate)
            .ThenBy(booking => booking.CourtSchedule.StartTime)
            .Select(booking => MapBookingResponse(booking))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BookingResponse>> GetBookingsAsync(
        BookingQueryParameters query,
        CancellationToken cancellationToken)
    {
        var bookingsQuery = GetBookingResponseQuery();

        if (query.FromDate.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(booking => booking.BookingDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(booking => booking.BookingDate <= query.ToDate.Value);
        }

        if (query.CourtId.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(booking => booking.CourtSchedule.CourtId == query.CourtId.Value);
        }

        if (query.Status.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(booking => booking.Status == query.Status.Value);
        }

        return await bookingsQuery
            .OrderByDescending(booking => booking.BookingDate)
            .ThenBy(booking => booking.CourtSchedule.StartTime)
            .Select(booking => MapBookingResponse(booking))
            .ToListAsync(cancellationToken);
    }

    public async Task<BookingResponse?> GetBookingByIdAsync(
        Guid id,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var query = GetBookingResponseQuery().Where(booking => booking.Id == id);

        if (!isAdmin)
        {
            query = query.Where(booking => booking.UserId == currentUserId);
        }

        return await query
            .Select(booking => MapBookingResponse(booking))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BookingResult<BookingResponse>> UpdateBookingStatusAsync(
        Guid id,
        BookingUpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await dbContext.Bookings
            .FirstOrDefaultAsync(booking => booking.Id == id && booking.DeletedAt == null, cancellationToken);

        if (booking is null)
        {
            return BookingResult<BookingResponse>.NotFound("Booking was not found.");
        }

        if (BlockingStatuses.Contains(request.Status))
        {
            var isBooked = await dbContext.Bookings.AnyAsync(otherBooking =>
                otherBooking.Id != booking.Id &&
                otherBooking.CourtScheduleId == booking.CourtScheduleId &&
                otherBooking.BookingDate == booking.BookingDate &&
                otherBooking.DeletedAt == null &&
                BlockingStatuses.Contains(otherBooking.Status),
                cancellationToken);

            if (isBooked)
            {
                return BookingResult<BookingResponse>.Conflict("This schedule has already been booked.");
            }
        }

        booking.Status = request.Status;
        booking.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateBookingException(exception))
        {
            return BookingResult<BookingResponse>.Conflict("This schedule has already been booked.");
        }

        var response = await GetBookingResponseQuery()
            .Where(existingBooking => existingBooking.Id == booking.Id)
            .Select(existingBooking => MapBookingResponse(existingBooking))
            .FirstAsync(cancellationToken);

        return BookingResult<BookingResponse>.Success(response);
    }

    private async Task<BookingResult<BookingResponse>> ValidateCreateRequestAsync(
        Guid userId,
        BookingCreateRequest request,
        CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users
            .AnyAsync(user => user.Id == userId && user.DeletedAt == null, cancellationToken);

        if (!userExists)
        {
            return BookingResult<BookingResponse>.BadRequest("User was not found.");
        }

        var schedule = await GetScheduleForBookingAsync(request.CourtScheduleId, cancellationToken);

        if (schedule is null)
        {
            return BookingResult<BookingResponse>.BadRequest("Schedule was not found.");
        }

        if (!schedule.IsAvailable)
        {
            return BookingResult<BookingResponse>.BadRequest("Schedule is not available.");
        }

        if (schedule.Court.Status != CourtStatus.Active)
        {
            return BookingResult<BookingResponse>.BadRequest("Court is not active.");
        }

        if (request.BookingDate.DayOfWeek != schedule.DayOfWeek)
        {
            return BookingResult<BookingResponse>.BadRequest("Booking date does not match schedule day of week.");
        }

        var isBooked = await dbContext.Bookings.AnyAsync(booking =>
            booking.CourtScheduleId == request.CourtScheduleId &&
            booking.BookingDate == request.BookingDate &&
            booking.DeletedAt == null &&
            BlockingStatuses.Contains(booking.Status),
            cancellationToken);

        if (isBooked)
        {
            return BookingResult<BookingResponse>.Conflict("This schedule has already been booked.");
        }

        return BookingResult<BookingResponse>.Success(new BookingResponse());
    }

    private async Task<CourtSchedule?> GetScheduleForBookingAsync(
        Guid scheduleId,
        CancellationToken cancellationToken)
    {
        return await dbContext.CourtSchedules
            .Include(schedule => schedule.Court)
            .ThenInclude(court => court.Venue)
            .Include(schedule => schedule.Court)
            .ThenInclude(court => court.Sport)
            .FirstOrDefaultAsync(schedule =>
                schedule.Id == scheduleId &&
                schedule.DeletedAt == null &&
                schedule.Court.DeletedAt == null &&
                schedule.Court.Venue.DeletedAt == null &&
                schedule.Court.Sport.DeletedAt == null,
                cancellationToken);
    }

    private IQueryable<Booking> GetBookingResponseQuery()
    {
        return dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.User)
            .Include(booking => booking.CourtSchedule)
            .ThenInclude(schedule => schedule.Court)
            .Where(booking => booking.DeletedAt == null);
    }

    private static BookingResponse MapBookingResponse(Booking booking)
    {
        return new BookingResponse
        {
            Id = booking.Id,
            UserId = booking.UserId,
            UserName = booking.User.FullName,
            CourtScheduleId = booking.CourtScheduleId,
            CourtName = booking.CourtSchedule.Court.Name,
            BookingDate = booking.BookingDate,
            Status = booking.Status,
            TotalPrice = booking.TotalPrice,
            Note = booking.Note,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
        };
    }

    private static bool IsDuplicateBookingException(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
    }
}

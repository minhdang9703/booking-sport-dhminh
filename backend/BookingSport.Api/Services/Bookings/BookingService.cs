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
            return MapValidationFailure(validation);
        }

        var priceRule = validation.Value!;
        var totalPrice = CalculateTotalPrice(request.StartTime, request.EndTime, priceRule.HourlyPrice);
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourtId = request.CourtId,
            BookingDate = request.BookingDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            HourlyPriceSnapshot = priceRule.HourlyPrice,
            TotalPrice = totalPrice,
            Status = BookingStatus.Pending,
            PaymentType = request.PaymentType,
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
            return BookingResult<BookingResponse>.Conflict("This time range has already been booked.");
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
            .ThenBy(booking => booking.StartTime)
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
            bookingsQuery = bookingsQuery.Where(booking => booking.CourtId == query.CourtId.Value);
        }

        if (query.Status.HasValue)
        {
            bookingsQuery = bookingsQuery.Where(booking => booking.Status == query.Status.Value);
        }

        return await bookingsQuery
            .OrderByDescending(booking => booking.BookingDate)
            .ThenBy(booking => booking.StartTime)
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
                otherBooking.CourtId == booking.CourtId &&
                otherBooking.BookingDate == booking.BookingDate &&
                otherBooking.StartTime < booking.EndTime &&
                otherBooking.EndTime > booking.StartTime &&
                otherBooking.DeletedAt == null &&
                BlockingStatuses.Contains(otherBooking.Status),
                cancellationToken);

            if (isBooked)
            {
                return BookingResult<BookingResponse>.Conflict("This time range has already been booked.");
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
            return BookingResult<BookingResponse>.Conflict("This time range has already been booked.");
        }

        var response = await GetBookingResponseQuery()
            .Where(existingBooking => existingBooking.Id == booking.Id)
            .Select(existingBooking => MapBookingResponse(existingBooking))
            .FirstAsync(cancellationToken);

        return BookingResult<BookingResponse>.Success(response);
    }

    private async Task<BookingResult<PriceRule>> ValidateCreateRequestAsync(
        Guid userId,
        BookingCreateRequest request,
        CancellationToken cancellationToken)
    {
        var userExists = await dbContext.Users
            .AnyAsync(user => user.Id == userId && user.DeletedAt == null, cancellationToken);

        if (!userExists)
        {
            return BookingResult<PriceRule>.BadRequest("User was not found.");
        }

        if (request.StartTime >= request.EndTime)
        {
            return BookingResult<PriceRule>.BadRequest("StartTime must be earlier than EndTime.");
        }

        var court = await dbContext.Courts
            .AsNoTracking()
            .FirstOrDefaultAsync(court =>
                court.Id == request.CourtId &&
                court.DeletedAt == null,
                cancellationToken);

        if (court is null)
        {
            return BookingResult<PriceRule>.BadRequest("Court was not found.");
        }

        if (court.Status != CourtStatus.Active)
        {
            return BookingResult<PriceRule>.BadRequest("Court is not active.");
        }

        var priceRule = await dbContext.PriceRules
            .AsNoTracking()
            .Where(rule =>
                rule.DayOfWeek == request.BookingDate.DayOfWeek &&
                rule.IsEnabled &&
                rule.StartTime <= request.StartTime &&
                rule.EndTime >= request.EndTime)
            .OrderBy(rule => rule.StartTime)
            .FirstOrDefaultAsync(cancellationToken);

        if (priceRule is null)
        {
            return BookingResult<PriceRule>.BadRequest("No enabled price rule covers this time range.");
        }

        var overlapsExistingBooking = await dbContext.Bookings.AnyAsync(booking =>
            booking.CourtId == request.CourtId &&
            booking.BookingDate == request.BookingDate &&
            booking.StartTime < request.EndTime &&
            booking.EndTime > request.StartTime &&
            booking.DeletedAt == null &&
            BlockingStatuses.Contains(booking.Status),
            cancellationToken);

        if (overlapsExistingBooking)
        {
            return BookingResult<PriceRule>.Conflict("This time range has already been booked.");
        }

        return BookingResult<PriceRule>.Success(priceRule);
    }

    private IQueryable<Booking> GetBookingResponseQuery()
    {
        return dbContext.Bookings
            .AsNoTracking()
            .Include(booking => booking.User)
            .Include(booking => booking.Court)
            .Where(booking => booking.DeletedAt == null);
    }

    private static BookingResponse MapBookingResponse(Booking booking)
    {
        return new BookingResponse
        {
            Id = booking.Id,
            UserId = booking.UserId,
            UserName = booking.User.FullName,
            CourtId = booking.CourtId,
            CourtName = booking.Court.Name,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            HourlyPriceSnapshot = booking.HourlyPriceSnapshot,
            Status = booking.Status,
            TotalPrice = booking.TotalPrice,
            PaymentType = booking.PaymentType,
            Note = booking.Note,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt
        };
    }

    private static decimal CalculateTotalPrice(TimeOnly startTime, TimeOnly endTime, decimal hourlyPrice)
    {
        var hours = (decimal)(endTime.ToTimeSpan() - startTime.ToTimeSpan()).TotalHours;

        return Math.Round(hours * hourlyPrice, 2);
    }

    private static bool IsDuplicateBookingException(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException &&
            (postgresException.SqlState == PostgresErrorCodes.UniqueViolation ||
             postgresException.SqlState == PostgresErrorCodes.ExclusionViolation);
    }

    private static BookingResult<BookingResponse> MapValidationFailure(BookingResult<PriceRule> result)
    {
        return result.Status switch
        {
            BookingResultStatus.BadRequest => BookingResult<BookingResponse>.BadRequest(result.Error ?? "Invalid booking request."),
            BookingResultStatus.Conflict => BookingResult<BookingResponse>.Conflict(result.Error ?? "Booking request conflicts with existing data."),
            BookingResultStatus.NotFound => BookingResult<BookingResponse>.NotFound(result.Error ?? "Booking data was not found."),
            _ => BookingResult<BookingResponse>.BadRequest(result.Error ?? "Invalid booking request.")
        };
    }
}

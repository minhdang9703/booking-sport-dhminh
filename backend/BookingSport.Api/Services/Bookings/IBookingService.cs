using BookingSport.Api.DTOs.Bookings;

namespace BookingSport.Api.Services.Bookings;

public interface IBookingService
{
    Task<BookingResult<BookingResponse>> CreateBookingAsync(
        Guid userId,
        BookingCreateRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BookingResponse>> GetMyBookingsAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<BookingResponse?> GetBookingByIdAsync(
        Guid id,
        Guid currentUserId,
        bool isAdmin,
        CancellationToken cancellationToken);
}

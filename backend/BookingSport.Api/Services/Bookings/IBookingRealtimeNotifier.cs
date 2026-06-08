using BookingSport.Api.DTOs.Bookings;

namespace BookingSport.Api.Services.Bookings;

public interface IBookingRealtimeNotifier
{
    Task NotifyBookingCreatedAsync(BookingResponse booking, CancellationToken cancellationToken);

    Task NotifyBookingStatusUpdatedAsync(BookingResponse booking, CancellationToken cancellationToken);
}

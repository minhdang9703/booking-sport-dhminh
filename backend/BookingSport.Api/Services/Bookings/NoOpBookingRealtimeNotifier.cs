using BookingSport.Api.DTOs.Bookings;

namespace BookingSport.Api.Services.Bookings;

public sealed class NoOpBookingRealtimeNotifier : IBookingRealtimeNotifier
{
    public Task NotifyBookingCreatedAsync(BookingResponse booking, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task NotifyBookingStatusUpdatedAsync(BookingResponse booking, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

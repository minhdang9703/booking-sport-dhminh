using BookingSport.Api.DTOs.Bookings;
using BookingSport.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BookingSport.Api.Services.Bookings;

public sealed class SignalRBookingRealtimeNotifier(IHubContext<BookingHub> hubContext) : IBookingRealtimeNotifier
{
    public Task NotifyBookingCreatedAsync(BookingResponse booking, CancellationToken cancellationToken)
    {
        return SendBookingEventAsync("BookingCreated", booking, cancellationToken);
    }

    public Task NotifyBookingStatusUpdatedAsync(BookingResponse booking, CancellationToken cancellationToken)
    {
        return SendBookingEventAsync("BookingStatusUpdated", booking, cancellationToken);
    }

    private async Task SendBookingEventAsync(
        string eventName,
        BookingResponse booking,
        CancellationToken cancellationToken)
    {
        var payload = new BookingRealtimeEvent
        {
            EventType = eventName,
            BookingId = booking.Id,
            UserId = booking.UserId,
            CourtId = booking.CourtId,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime,
            EndTime = booking.EndTime,
            Status = booking.Status,
            Booking = booking
        };

        await hubContext.Clients
            .Group(BookingHub.GetCourtGroupName(booking.CourtId))
            .SendAsync("BookingAvailabilityChanged", payload, cancellationToken);

        await hubContext.Clients
            .Group(BookingHub.AdminBookingsGroup)
            .SendAsync(eventName, payload, cancellationToken);

        await hubContext.Clients
            .Group(BookingHub.GetUserGroupName(booking.UserId))
            .SendAsync(eventName, payload, cancellationToken);
    }
}

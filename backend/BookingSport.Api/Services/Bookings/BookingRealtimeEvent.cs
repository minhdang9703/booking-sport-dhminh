using BookingSport.Api.DTOs.Bookings;
using BookingSport.Api.Enums;

namespace BookingSport.Api.Services.Bookings;

public sealed class BookingRealtimeEvent
{
    public string EventType { get; init; } = string.Empty;
    public Guid BookingId { get; init; }
    public Guid UserId { get; init; }
    public Guid CourtId { get; init; }
    public DateOnly BookingDate { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public BookingStatus Status { get; init; }
    public BookingResponse? Booking { get; init; }
}

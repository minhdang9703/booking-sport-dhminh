using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Bookings;

public class BookingCreateRequest
{
    public Guid CourtId { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public PaymentType PaymentType { get; set; } = PaymentType.Cash;
    public string? Note { get; set; }
}

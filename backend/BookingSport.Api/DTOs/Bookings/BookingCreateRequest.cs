namespace BookingSport.Api.DTOs.Bookings;

public class BookingCreateRequest
{
    public Guid CourtScheduleId { get; set; }
    public DateOnly BookingDate { get; set; }
    public string? Note { get; set; }
}

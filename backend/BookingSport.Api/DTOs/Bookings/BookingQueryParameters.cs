using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Bookings;

public class BookingQueryParameters
{
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public Guid? CourtId { get; set; }
    public BookingStatus? Status { get; set; }
}

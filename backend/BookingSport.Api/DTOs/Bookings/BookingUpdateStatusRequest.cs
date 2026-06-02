using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Bookings;

public class BookingUpdateStatusRequest
{
    public BookingStatus Status { get; set; }
}

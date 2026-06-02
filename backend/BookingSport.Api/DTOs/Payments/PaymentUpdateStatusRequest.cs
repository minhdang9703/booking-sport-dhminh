using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Payments;

public class PaymentUpdateStatusRequest
{
    public PaymentStatus Status { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

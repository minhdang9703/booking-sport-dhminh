using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Payments;

public class PaymentCreateRequest
{
    public Guid BookingId { get; set; }
    public PaymentMethod Method { get; set; }
    public decimal Amount { get; set; }
    public string? TransactionCode { get; set; }
}

using BookingSport.Api.Enums;

namespace BookingSport.Api.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public decimal Amount { get; set; }
    public string? TransactionCode { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public Booking Booking { get; set; } = null!;
}

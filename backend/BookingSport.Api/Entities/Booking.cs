using BookingSport.Api.Enums;

namespace BookingSport.Api.Entities;

public class Booking
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CourtScheduleId { get; set; }
    public DateOnly BookingDate { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public decimal TotalPrice { get; set; }
    public string? Note { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public CourtSchedule CourtSchedule { get; set; } = null!;
    public Payment? Payment { get; set; }
}

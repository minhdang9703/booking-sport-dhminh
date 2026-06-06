using BookingSport.Api.Entities;
using BookingSport.Api.Enums;

namespace BookingSport.Api.Tests.TestSupport;

public static class TestData
{
    public static User User(
        Guid? id = null,
        string fullName = "Customer One",
        string email = "customer@example.com",
        string phoneNumber = "0900000001",
        UserRole role = UserRole.Customer,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? deletedAt = null)
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            PasswordHash = "hashed-password",
            Role = role,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow.AddDays(-3),
            DeletedAt = deletedAt
        };
    }

    public static Court Court(
        Guid? id = null,
        string name = "Court A",
        string courtType = "Badminton",
        CourtStatus status = CourtStatus.Active,
        DateTimeOffset? deletedAt = null)
    {
        return new Court
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            CourtType = courtType,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            DeletedAt = deletedAt
        };
    }

    public static PriceRule PriceRule(
        Guid? id = null,
        string name = "Weekday morning",
        DayOfWeek dayOfWeek = DayOfWeek.Monday,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        decimal hourlyPrice = 100_000m,
        bool isEnabled = true)
    {
        return new PriceRule
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            DayOfWeek = dayOfWeek,
            StartTime = startTime ?? new TimeOnly(8, 0),
            EndTime = endTime ?? new TimeOnly(12, 0),
            HourlyPrice = hourlyPrice,
            IsEnabled = isEnabled,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
    }

    public static Booking Booking(
        Guid? id = null,
        Guid? userId = null,
        Guid? courtId = null,
        DateOnly? bookingDate = null,
        TimeOnly? startTime = null,
        TimeOnly? endTime = null,
        BookingStatus status = BookingStatus.Pending,
        decimal hourlyPriceSnapshot = 100_000m,
        decimal totalPrice = 100_000m,
        DateTimeOffset? deletedAt = null)
    {
        return new Booking
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            CourtId = courtId ?? Guid.NewGuid(),
            BookingDate = bookingDate ?? new DateOnly(2026, 6, 8),
            StartTime = startTime ?? new TimeOnly(8, 0),
            EndTime = endTime ?? new TimeOnly(9, 0),
            Status = status,
            HourlyPriceSnapshot = hourlyPriceSnapshot,
            TotalPrice = totalPrice,
            PaymentType = PaymentType.Cash,
            CreatedAt = DateTimeOffset.UtcNow,
            DeletedAt = deletedAt
        };
    }
}

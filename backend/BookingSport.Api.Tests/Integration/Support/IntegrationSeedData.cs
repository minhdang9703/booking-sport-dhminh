using BookingSport.Api.Data;
using BookingSport.Api.Entities;
using BookingSport.Api.Enums;
using Microsoft.AspNetCore.Identity;

namespace BookingSport.Api.Tests.Integration.Support;

public static class IntegrationSeedData
{
    public const string Password = "Test@123456";

    public static readonly DateOnly BookingDate = new(2026, 6, 8);

    public static SeedIds AddCoreData(AppDbContext dbContext)
    {
        var hasher = new PasswordHasher<User>();
        var admin = User(
            fullName: "Integration Admin",
            email: "admin.integration@test.local",
            phoneNumber: "0911000001",
            role: UserRole.Admin);
        var customer = User(
            fullName: "Integration Customer",
            email: "customer.integration@test.local",
            phoneNumber: "0911000002",
            role: UserRole.Customer);
        admin.PasswordHash = hasher.HashPassword(admin, Password);
        customer.PasswordHash = hasher.HashPassword(customer, Password);

        var court = Court(name: "Integration Court Alpha", courtType: "Badminton");
        var inactiveCourt = Court(name: "Integration Court Inactive", courtType: "Tennis", status: CourtStatus.Inactive);
        var priceRule = PriceRule(
            name: "Integration Monday Morning",
            dayOfWeek: BookingDate.DayOfWeek,
            startTime: new TimeOnly(8, 0),
            endTime: new TimeOnly(12, 0),
            hourlyPrice: 120_000m);

        dbContext.Users.AddRange(admin, customer);
        dbContext.Courts.AddRange(court, inactiveCourt);
        dbContext.PriceRules.Add(priceRule);

        return new SeedIds(admin.Id, customer.Id, court.Id, inactiveCourt.Id, priceRule.Id);
    }

    public static User User(
        string fullName,
        string email,
        string phoneNumber,
        UserRole role,
        DateTimeOffset? deletedAt = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            Role = role,
            PasswordHash = "hashed",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-5),
            DeletedAt = deletedAt
        };
    }

    public static Court Court(
        string name,
        string courtType,
        CourtStatus status = CourtStatus.Active,
        DateTimeOffset? deletedAt = null)
    {
        return new Court
        {
            Id = Guid.NewGuid(),
            Name = name,
            CourtType = courtType,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-4),
            DeletedAt = deletedAt
        };
    }

    public static PriceRule PriceRule(
        string name,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        decimal hourlyPrice,
        bool isEnabled = true)
    {
        return new PriceRule
        {
            Id = Guid.NewGuid(),
            Name = name,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            HourlyPrice = hourlyPrice,
            IsEnabled = isEnabled,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-3)
        };
    }

    public static Booking Booking(
        Guid userId,
        Guid courtId,
        DateOnly bookingDate,
        TimeOnly startTime,
        TimeOnly endTime,
        BookingStatus status,
        decimal hourlyPriceSnapshot = 120_000m,
        decimal totalPrice = 120_000m,
        DateTimeOffset? deletedAt = null)
    {
        return new Booking
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourtId = courtId,
            BookingDate = bookingDate,
            StartTime = startTime,
            EndTime = endTime,
            Status = status,
            HourlyPriceSnapshot = hourlyPriceSnapshot,
            TotalPrice = totalPrice,
            PaymentType = PaymentType.Cash,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            DeletedAt = deletedAt
        };
    }
}

public sealed record SeedIds(
    Guid AdminId,
    Guid CustomerId,
    Guid CourtId,
    Guid InactiveCourtId,
    Guid PriceRuleId);

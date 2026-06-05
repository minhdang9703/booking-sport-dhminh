using BookingSport.Api.Data;
using BookingSport.Api.Entities;
using BookingSport.Api.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

const string defaultConnectionString =
    "Host=localhost;Port=5432;Database=booking_sport;Username=postgres;Password=root123";
const string seedNotePrefix = "[seed-018]";
const string samplePassword = "Test@123456";

var connectionString = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
    ? args[0]
    : defaultConnectionString;

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var dbContext = new AppDbContext(options);
await dbContext.Database.OpenConnectionAsync();

Console.WriteLine("Connected to booking_sport database.");

var passwordHasher = new PasswordHasher<User>();
var now = DateTimeOffset.UtcNow;

var admin = await UpsertUserAsync(
    dbContext,
    passwordHasher,
    fullName: "Admin Test",
    email: "admin@test.local",
    phoneNumber: "0900000001",
    role: UserRole.Admin,
    now);

var owner = await UpsertUserAsync(
    dbContext,
    passwordHasher,
    fullName: "Owner Test",
    email: "owner@test.local",
    phoneNumber: "0900000002",
    role: UserRole.Owner,
    now);

var customer = await UpsertUserAsync(
    dbContext,
    passwordHasher,
    fullName: "Customer Test",
    email: "customer@test.local",
    phoneNumber: "0900000003",
    role: UserRole.Customer,
    now);

var courtA = await UpsertCourtAsync(dbContext, "Sân 5A", "Sân 5", CourtStatus.Active, now);
var courtB = await UpsertCourtAsync(dbContext, "Sân 5B", "Sân 5", CourtStatus.Active, now);

await UpsertPriceRulesAsync(dbContext, now);
await ResetSampleBookingsAsync(dbContext, customer, courtA, courtB, now);
await dbContext.SaveChangesAsync();

Console.WriteLine("Sample data inserted/upserted successfully.");
Console.WriteLine("Accounts:");
Console.WriteLine($"- admin@test.local / {samplePassword}");
Console.WriteLine($"- owner@test.local / {samplePassword}");
Console.WriteLine($"- customer@test.local / {samplePassword}");
Console.WriteLine("Courts:");
Console.WriteLine($"- {courtA.Name} ({courtA.Id})");
Console.WriteLine($"- {courtB.Name} ({courtB.Id})");

static async Task<User> UpsertUserAsync(
    AppDbContext dbContext,
    PasswordHasher<User> passwordHasher,
    string fullName,
    string email,
    string phoneNumber,
    UserRole role,
    DateTimeOffset now)
{
    var user = await dbContext.Users
        .FirstOrDefaultAsync(existingUser =>
            existingUser.Email == email ||
            existingUser.PhoneNumber == phoneNumber);

    if (user is null)
    {
        user = new User
        {
            Id = Guid.NewGuid(),
            CreatedAt = now
        };
        dbContext.Users.Add(user);
    }
    else
    {
        user.UpdatedAt = now;
        user.DeletedAt = null;
    }

    user.FullName = fullName;
    user.Email = email;
    user.PhoneNumber = phoneNumber;
    user.Role = role;
    user.PasswordHash = passwordHasher.HashPassword(user, samplePassword);

    return user;
}

static async Task<Court> UpsertCourtAsync(
    AppDbContext dbContext,
    string name,
    string courtType,
    CourtStatus status,
    DateTimeOffset now)
{
    var court = await dbContext.Courts
        .FirstOrDefaultAsync(existingCourt => existingCourt.Name == name);

    if (court is null)
    {
        court = new Court
        {
            Id = Guid.NewGuid(),
            CreatedAt = now
        };
        dbContext.Courts.Add(court);
    }
    else
    {
        court.UpdatedAt = now;
        court.DeletedAt = null;
    }

    court.Name = name;
    court.CourtType = courtType;
    court.Status = status;

    return court;
}

static async Task UpsertPriceRulesAsync(AppDbContext dbContext, DateTimeOffset now)
{
    var weekdays = new[]
    {
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    };

    foreach (var dayOfWeek in weekdays)
    {
        await UpsertPriceRuleAsync(
            dbContext,
            $"{dayOfWeek} regular 05:00-17:00",
            dayOfWeek,
            new TimeOnly(5, 0),
            new TimeOnly(17, 0),
            150_000m,
            now);

        await UpsertPriceRuleAsync(
            dbContext,
            $"{dayOfWeek} peak 17:00-22:00",
            dayOfWeek,
            new TimeOnly(17, 0),
            new TimeOnly(22, 0),
            250_000m,
            now);
    }

    foreach (var dayOfWeek in new[] { DayOfWeek.Saturday, DayOfWeek.Sunday })
    {
        await UpsertPriceRuleAsync(
            dbContext,
            $"{dayOfWeek} weekend 05:00-22:00",
            dayOfWeek,
            new TimeOnly(5, 0),
            new TimeOnly(22, 0),
            300_000m,
            now);
    }
}

static async Task UpsertPriceRuleAsync(
    AppDbContext dbContext,
    string name,
    DayOfWeek dayOfWeek,
    TimeOnly startTime,
    TimeOnly endTime,
    decimal hourlyPrice,
    DateTimeOffset now)
{
    var rule = await dbContext.PriceRules
        .FirstOrDefaultAsync(existingRule => existingRule.Name == name);

    if (rule is null)
    {
        rule = new PriceRule
        {
            Id = Guid.NewGuid(),
            CreatedAt = now
        };
        dbContext.PriceRules.Add(rule);
    }
    else
    {
        rule.UpdatedAt = now;
    }

    rule.Name = name;
    rule.DayOfWeek = dayOfWeek;
    rule.StartTime = startTime;
    rule.EndTime = endTime;
    rule.HourlyPrice = hourlyPrice;
    rule.IsEnabled = true;
}

static async Task ResetSampleBookingsAsync(
    AppDbContext dbContext,
    User customer,
    Court courtA,
    Court courtB,
    DateTimeOffset now)
{
    var existingSampleBookings = await dbContext.Bookings
        .Where(booking => booking.Note != null && booking.Note.StartsWith(seedNotePrefix))
        .ToListAsync();

    dbContext.Bookings.RemoveRange(existingSampleBookings);

    var today = DateOnly.FromDateTime(DateTime.Today);
    var saturday = GetNextOrCurrentDay(today, DayOfWeek.Saturday);

    dbContext.Bookings.AddRange(
        BuildBooking(customer, courtA, today, new TimeOnly(18, 0), new TimeOnly(19, 0), 250_000m, BookingStatus.Confirmed, PaymentType.Online, "Confirmed booking", now),
        BuildBooking(customer, courtA, today, new TimeOnly(19, 30), new TimeOnly(20, 30), 250_000m, BookingStatus.Pending, PaymentType.BankTransfer, "Pending booking", now),
        BuildBooking(customer, courtB, saturday, new TimeOnly(7, 0), new TimeOnly(8, 30), 300_000m, BookingStatus.Completed, PaymentType.Cash, "Completed booking", now),
        BuildBooking(customer, courtB, saturday, new TimeOnly(16, 0), new TimeOnly(17, 0), 300_000m, BookingStatus.Cancelled, PaymentType.Cash, "Cancelled booking", now));
}

static Booking BuildBooking(
    User user,
    Court court,
    DateOnly bookingDate,
    TimeOnly startTime,
    TimeOnly endTime,
    decimal hourlyPrice,
    BookingStatus status,
    PaymentType paymentType,
    string note,
    DateTimeOffset now)
{
    var hours = (decimal)(endTime.ToTimeSpan() - startTime.ToTimeSpan()).TotalHours;

    return new Booking
    {
        Id = Guid.NewGuid(),
        User = user,
        Court = court,
        UserId = user.Id,
        CourtId = court.Id,
        BookingDate = bookingDate,
        StartTime = startTime,
        EndTime = endTime,
        HourlyPriceSnapshot = hourlyPrice,
        TotalPrice = Math.Round(hours * hourlyPrice, 2),
        Status = status,
        PaymentType = paymentType,
        Note = $"{seedNotePrefix} {note}",
        CreatedAt = now
    };
}

static DateOnly GetNextOrCurrentDay(DateOnly date, DayOfWeek targetDay)
{
    var daysToAdd = ((int)targetDay - (int)date.DayOfWeek + 7) % 7;

    return date.AddDays(daysToAdd);
}

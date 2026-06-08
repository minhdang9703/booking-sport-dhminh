using BookingSport.Api.DTOs.Bookings;
using BookingSport.Api.Enums;
using BookingSport.Api.Services.Bookings;
using BookingSport.Api.Services.Jobs;
using BookingSport.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookingSport.Api.Tests.Services.Bookings;

public class BookingServiceTests
{
    private static readonly DateOnly Monday = new(2026, 6, 8);

    [Fact]
    public async Task CreateBookingAsync_WithValidRequest_CreatesPendingBookingWithCalculatedPrice()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.PriceRules.Add(TestData.PriceRule(dayOfWeek: Monday.DayOfWeek, hourlyPrice: 120_000m));
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.CreateBookingAsync(user.Id, new BookingCreateRequest
        {
            CourtId = court.Id,
            BookingDate = Monday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(10, 0),
            PaymentType = PaymentType.BankTransfer,
            Note = "  bring rackets "
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Status.Should().Be(BookingStatus.Pending);
        result.Value.HourlyPriceSnapshot.Should().Be(120_000m);
        result.Value.TotalPrice.Should().Be(180_000m);
        result.Value.Note.Should().Be("bring rackets");
        result.Value.UserName.Should().Be(user.FullName);
        result.Value.CourtName.Should().Be(court.Name);
    }

    [Fact]
    public async Task CreateBookingAsync_WithValidRequest_NotifiesRealtimeAndEnqueuesConfirmationEmail()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.PriceRules.Add(TestData.PriceRule(dayOfWeek: Monday.DayOfWeek));
        await db.Context.SaveChangesAsync();
        var notifier = new RecordingBookingRealtimeNotifier();
        var emailQueue = new RecordingBookingConfirmationEmailQueue();
        var service = new BookingService(db.Context, notifier, emailQueue, NullLogger<BookingService>.Instance);

        var result = await service.CreateBookingAsync(user.Id, ValidCreateRequest(court.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        notifier.CreatedBookingIds.Should().Contain(result.Value!.Id).And.HaveCount(1);
        emailQueue.BookingIds.Should().Contain(result.Value.Id).And.HaveCount(1);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenUserDoesNotExist_ReturnsBadRequest()
    {
        await using var db = await TestDb.CreateAsync();
        var service = CreateService(db.Context);

        var result = await service.CreateBookingAsync(Guid.NewGuid(), ValidCreateRequest(Guid.NewGuid()), CancellationToken.None);

        result.Status.Should().Be(BookingResultStatus.BadRequest);
        result.Error.Should().Be("User was not found.");
    }

    [Fact]
    public async Task CreateBookingAsync_WhenCourtIsInactive_ReturnsBadRequest()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court(status: CourtStatus.Inactive);
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.CreateBookingAsync(user.Id, ValidCreateRequest(court.Id), CancellationToken.None);

        result.Status.Should().Be(BookingResultStatus.BadRequest);
        result.Error.Should().Be("Court is not active.");
    }

    [Fact]
    public async Task CreateBookingAsync_WhenNoPriceRuleCoversRange_ReturnsBadRequest()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.PriceRules.Add(TestData.PriceRule(dayOfWeek: Monday.DayOfWeek, startTime: new TimeOnly(8, 0), endTime: new TimeOnly(9, 0)));
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.CreateBookingAsync(user.Id, new BookingCreateRequest
        {
            CourtId = court.Id,
            BookingDate = Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0)
        }, CancellationToken.None);

        result.Status.Should().Be(BookingResultStatus.BadRequest);
        result.Error.Should().Be("No enabled price rule covers this time range.");
    }

    [Fact]
    public async Task CreateBookingAsync_WhenOverlapsBlockingBooking_ReturnsConflict()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.PriceRules.Add(TestData.PriceRule(dayOfWeek: Monday.DayOfWeek));
        db.Context.Bookings.Add(TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday, startTime: new TimeOnly(9, 0), endTime: new TimeOnly(10, 0), status: BookingStatus.Confirmed));
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.CreateBookingAsync(user.Id, new BookingCreateRequest
        {
            CourtId = court.Id,
            BookingDate = Monday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(9, 30)
        }, CancellationToken.None);

        result.Status.Should().Be(BookingResultStatus.Conflict);
    }

    [Fact]
    public async Task CreateBookingAsync_WhenOverlapsBlockingBooking_DoesNotNotifyOrEnqueueEmail()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.PriceRules.Add(TestData.PriceRule(dayOfWeek: Monday.DayOfWeek));
        db.Context.Bookings.Add(TestData.Booking(
            userId: user.Id,
            courtId: court.Id,
            bookingDate: Monday,
            startTime: new TimeOnly(9, 0),
            endTime: new TimeOnly(10, 0),
            status: BookingStatus.Confirmed));
        await db.Context.SaveChangesAsync();
        var notifier = new RecordingBookingRealtimeNotifier();
        var emailQueue = new RecordingBookingConfirmationEmailQueue();
        var service = new BookingService(db.Context, notifier, emailQueue, NullLogger<BookingService>.Instance);

        var result = await service.CreateBookingAsync(user.Id, new BookingCreateRequest
        {
            CourtId = court.Id,
            BookingDate = Monday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(9, 30)
        }, CancellationToken.None);

        result.Status.Should().Be(BookingResultStatus.Conflict);
        notifier.CreatedBookingIds.Should().BeEmpty();
        emailQueue.BookingIds.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateBookingAsync_WhenOverlapsCompletedBooking_CreatesBooking()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.PriceRules.Add(TestData.PriceRule(dayOfWeek: Monday.DayOfWeek));
        db.Context.Bookings.Add(TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday, startTime: new TimeOnly(9, 0), endTime: new TimeOnly(10, 0), status: BookingStatus.Completed));
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.CreateBookingAsync(user.Id, new BookingCreateRequest
        {
            CourtId = court.Id,
            BookingDate = Monday,
            StartTime = new TimeOnly(8, 30),
            EndTime = new TimeOnly(9, 30)
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetBookingByIdAsync_WhenCurrentUserDoesNotOwnBooking_ReturnsNull()
    {
        await using var db = await TestDb.CreateAsync();
        var owner = TestData.User();
        var other = TestData.User(email: "other@example.com", phoneNumber: "0900000002");
        var court = TestData.Court();
        var booking = TestData.Booking(userId: owner.Id, courtId: court.Id, bookingDate: Monday);
        db.Context.Users.AddRange(owner, other);
        db.Context.Courts.Add(court);
        db.Context.Bookings.Add(booking);
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.GetBookingByIdAsync(booking.Id, other.Id, isAdmin: false, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBookingsAsync_FiltersByDateCourtAndStatus()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        var otherCourt = TestData.Court(name: "Court B");
        db.Context.Users.Add(user);
        db.Context.Courts.AddRange(court, otherCourt);
        db.Context.Bookings.AddRange(
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday, status: BookingStatus.Completed),
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday.AddDays(1), status: BookingStatus.Completed),
            TestData.Booking(userId: user.Id, courtId: otherCourt.Id, bookingDate: Monday, status: BookingStatus.Completed),
            TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday, status: BookingStatus.Pending));
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.GetBookingsAsync(new BookingQueryParameters
        {
            FromDate = Monday,
            ToDate = Monday,
            CourtId = court.Id,
            Status = BookingStatus.Completed
        }, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].CourtId.Should().Be(court.Id);
    }

    [Fact]
    public async Task UpdateBookingStatusAsync_WhenChangingToBlockingStatusAndOverlapExists_ReturnsConflict()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        var court = TestData.Court();
        var booking = TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday, startTime: new TimeOnly(8, 0), endTime: new TimeOnly(9, 0), status: BookingStatus.Cancelled);
        var otherBooking = TestData.Booking(userId: user.Id, courtId: court.Id, bookingDate: Monday, startTime: new TimeOnly(8, 30), endTime: new TimeOnly(9, 30), status: BookingStatus.Pending);
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.Bookings.AddRange(booking, otherBooking);
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.UpdateBookingStatusAsync(booking.Id, new BookingUpdateStatusRequest
        {
            Status = BookingStatus.Confirmed
        }, CancellationToken.None);

        result.Status.Should().Be(BookingResultStatus.Conflict);
    }

    private static BookingCreateRequest ValidCreateRequest(Guid courtId)
    {
        return new BookingCreateRequest
        {
            CourtId = courtId,
            BookingDate = Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(9, 0)
        };
    }

    private static BookingService CreateService(BookingSport.Api.Data.AppDbContext dbContext)
    {
        return new BookingService(
            dbContext,
            new NoOpBookingRealtimeNotifier(),
            new NoOpBookingConfirmationEmailQueue(),
            NullLogger<BookingService>.Instance);
    }

    private sealed class RecordingBookingRealtimeNotifier : IBookingRealtimeNotifier
    {
        public List<Guid> CreatedBookingIds { get; } = [];
        public List<Guid> UpdatedBookingIds { get; } = [];

        public Task NotifyBookingCreatedAsync(BookingResponse booking, CancellationToken cancellationToken)
        {
            CreatedBookingIds.Add(booking.Id);
            return Task.CompletedTask;
        }

        public Task NotifyBookingStatusUpdatedAsync(BookingResponse booking, CancellationToken cancellationToken)
        {
            UpdatedBookingIds.Add(booking.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBookingConfirmationEmailQueue : IBookingConfirmationEmailQueue
    {
        public List<Guid> BookingIds { get; } = [];

        public void EnqueueBookingConfirmation(Guid bookingId)
        {
            BookingIds.Add(bookingId);
        }
    }
}

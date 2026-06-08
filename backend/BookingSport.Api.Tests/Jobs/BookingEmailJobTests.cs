using BookingSport.Api.Services.Email;
using BookingSport.Api.Services.Jobs;
using BookingSport.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookingSport.Api.Tests.Jobs;

public class BookingEmailJobTests
{
    [Fact]
    public async Task SendBookingConfirmationAsync_WhenBookingExists_SendsEmail()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User(email: "booking@example.com");
        var court = TestData.Court();
        var booking = TestData.Booking(userId: user.Id, courtId: court.Id);
        db.Context.Users.Add(user);
        db.Context.Courts.Add(court);
        db.Context.Bookings.Add(booking);
        await db.Context.SaveChangesAsync();
        var emailSender = new RecordingEmailSender();
        var job = new BookingEmailJob(db.Context, emailSender, NullLogger<BookingEmailJob>.Instance);

        await job.SendBookingConfirmationAsync(booking.Id, CancellationToken.None);

        emailSender.Messages.Should().ContainSingle();
        emailSender.Messages[0].ToEmail.Should().Be(user.Email);
        emailSender.Messages[0].Subject.Should().Contain(court.Name);
        emailSender.Messages[0].TextBody.Should().Contain(booking.Id.ToString());
    }

    [Fact]
    public async Task SendBookingConfirmationAsync_WhenBookingDoesNotExist_DoesNotSendEmail()
    {
        await using var db = await TestDb.CreateAsync();
        var emailSender = new RecordingEmailSender();
        var job = new BookingEmailJob(db.Context, emailSender, NullLogger<BookingEmailJob>.Instance);

        await job.SendBookingConfirmationAsync(Guid.NewGuid(), CancellationToken.None);

        emailSender.Messages.Should().BeEmpty();
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}

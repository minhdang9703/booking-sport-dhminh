using Hangfire;

namespace BookingSport.Api.Services.Jobs;

public sealed class HangfireBookingConfirmationEmailQueue(
    IBackgroundJobClient backgroundJobClient,
    ILogger<HangfireBookingConfirmationEmailQueue> logger) : IBookingConfirmationEmailQueue
{
    public void EnqueueBookingConfirmation(Guid bookingId)
    {
        backgroundJobClient.Enqueue<IBookingEmailJob>(
            job => job.SendBookingConfirmationAsync(bookingId, CancellationToken.None));

        logger.LogInformation("Booking confirmation email job was enqueued for booking {BookingId}.", bookingId);
    }
}

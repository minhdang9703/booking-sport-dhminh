namespace BookingSport.Api.Services.Jobs;

public sealed class NoOpBookingConfirmationEmailQueue : IBookingConfirmationEmailQueue
{
    public void EnqueueBookingConfirmation(Guid bookingId)
    {
    }
}

namespace BookingSport.Api.Services.Jobs;

public interface IBookingConfirmationEmailQueue
{
    void EnqueueBookingConfirmation(Guid bookingId);
}

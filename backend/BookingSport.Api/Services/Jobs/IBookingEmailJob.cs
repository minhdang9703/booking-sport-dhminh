namespace BookingSport.Api.Services.Jobs;

public interface IBookingEmailJob
{
    Task SendBookingConfirmationAsync(Guid bookingId, CancellationToken cancellationToken);
}

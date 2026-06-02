namespace BookingSport.Api.Services.Availability;

public interface IAvailabilityService
{
    Task<AvailabilityResult> GetAvailableSchedulesAsync(
        Guid courtId,
        DateOnly date,
        CancellationToken cancellationToken);
}

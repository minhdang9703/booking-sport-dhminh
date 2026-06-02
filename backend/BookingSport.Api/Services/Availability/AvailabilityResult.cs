using BookingSport.Api.DTOs.Availability;

namespace BookingSport.Api.Services.Availability;

public class AvailabilityResult
{
    private AvailabilityResult(bool found, IReadOnlyList<AvailableScheduleResponse> schedules)
    {
        Found = found;
        Schedules = schedules;
    }

    public bool Found { get; }
    public IReadOnlyList<AvailableScheduleResponse> Schedules { get; }

    public static AvailabilityResult Success(IReadOnlyList<AvailableScheduleResponse> schedules)
    {
        return new AvailabilityResult(true, schedules);
    }

    public static AvailabilityResult NotFound()
    {
        return new AvailabilityResult(false, Array.Empty<AvailableScheduleResponse>());
    }
}

using BookingSport.Api.DTOs.CourtSchedules;

namespace BookingSport.Api.Services.CourtSchedules;

public interface ICourtScheduleService
{
    Task<IReadOnlyList<CourtScheduleResponse>> GetCourtSchedulesAsync(
        CourtScheduleQueryParameters query,
        CancellationToken cancellationToken);

    Task<CourtScheduleResponse?> GetCourtScheduleByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CourtScheduleResult<CourtScheduleResponse>> CreateCourtScheduleAsync(
        CourtScheduleCreateRequest request,
        CancellationToken cancellationToken);

    Task<CourtScheduleResult<CourtScheduleResponse>> UpdateCourtScheduleAsync(
        Guid id,
        CourtScheduleUpdateRequest request,
        CancellationToken cancellationToken);

    Task<CourtScheduleResult<bool>> DeleteCourtScheduleAsync(Guid id, CancellationToken cancellationToken);
}

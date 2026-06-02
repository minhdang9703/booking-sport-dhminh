using BookingSport.Api.DTOs.Courts;

namespace BookingSport.Api.Services.Courts;

public interface ICourtService
{
    Task<IReadOnlyList<CourtResponse>> GetCourtsAsync(
        CourtQueryParameters query,
        CancellationToken cancellationToken);

    Task<CourtResponse?> GetCourtByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<CourtResult<CourtResponse>> CreateCourtAsync(
        CourtCreateRequest request,
        CancellationToken cancellationToken);

    Task<CourtResult<CourtResponse>> UpdateCourtAsync(
        Guid id,
        CourtUpdateRequest request,
        CancellationToken cancellationToken);

    Task<CourtResult<bool>> DeleteCourtAsync(Guid id, CancellationToken cancellationToken);
}

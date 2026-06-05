using BookingSport.Api.Data;
using BookingSport.Api.DTOs.Courts;
using BookingSport.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Services.Courts;

public class CourtService(AppDbContext dbContext) : ICourtService
{
    public async Task<IReadOnlyList<CourtResponse>> GetCourtsAsync(
        CourtQueryParameters query,
        CancellationToken cancellationToken)
    {
        var courtsQuery = BaseCourtQuery();

        if (query.Status.HasValue)
        {
            courtsQuery = courtsQuery.Where(court => court.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.CourtType))
        {
            var courtType = query.CourtType.Trim();
            courtsQuery = courtsQuery.Where(court => EF.Functions.ILike(court.CourtType, $"%{courtType}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            courtsQuery = courtsQuery.Where(court =>
                EF.Functions.ILike(court.Name, $"%{keyword}%") ||
                EF.Functions.ILike(court.CourtType, $"%{keyword}%"));
        }

        return await courtsQuery
            .OrderBy(court => court.Name)
            .Select(court => MapCourtResponse(court))
            .ToListAsync(cancellationToken);
    }

    public async Task<CourtResponse?> GetCourtByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await BaseCourtQuery()
            .Where(court => court.Id == id)
            .Select(court => MapCourtResponse(court))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CourtResult<CourtResponse>> CreateCourtAsync(
        CourtCreateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateRequestAsync(null, request.Name, request.CourtType, cancellationToken);

        if (!validation.Succeeded)
        {
            return validation;
        }

        var court = new Court
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            CourtType = request.CourtType.Trim(),
            Status = request.Status,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Courts.Add(court);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CourtResult<CourtResponse>.Success(MapCourtResponse(court));
    }

    public async Task<CourtResult<CourtResponse>> UpdateCourtAsync(
        Guid id,
        CourtUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var court = await dbContext.Courts
            .FirstOrDefaultAsync(court => court.Id == id && court.DeletedAt == null, cancellationToken);

        if (court is null)
        {
            return CourtResult<CourtResponse>.NotFound("Court was not found.");
        }

        var validation = await ValidateRequestAsync(id, request.Name, request.CourtType, cancellationToken);

        if (!validation.Succeeded)
        {
            return validation;
        }

        court.Name = request.Name.Trim();
        court.CourtType = request.CourtType.Trim();
        court.Status = request.Status;
        court.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return CourtResult<CourtResponse>.Success(MapCourtResponse(court));
    }

    public async Task<CourtResult<bool>> DeleteCourtAsync(Guid id, CancellationToken cancellationToken)
    {
        var court = await dbContext.Courts
            .FirstOrDefaultAsync(court => court.Id == id && court.DeletedAt == null, cancellationToken);

        if (court is null)
        {
            return CourtResult<bool>.NotFound("Court was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        court.DeletedAt = now;
        court.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return CourtResult<bool>.Success(true);
    }

    private IQueryable<Court> BaseCourtQuery()
    {
        return dbContext.Courts
            .AsNoTracking()
            .Where(court => court.DeletedAt == null);
    }

    private async Task<CourtResult<CourtResponse>> ValidateRequestAsync(
        Guid? id,
        string name,
        string courtType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return CourtResult<CourtResponse>.BadRequest("Court name is required.");
        }

        if (string.IsNullOrWhiteSpace(courtType))
        {
            return CourtResult<CourtResponse>.BadRequest("CourtType is required.");
        }

        var trimmedName = name.Trim();
        var nameExists = await dbContext.Courts.AnyAsync(court =>
            (!id.HasValue || court.Id != id.Value) &&
            court.DeletedAt == null &&
            court.Name == trimmedName,
            cancellationToken);

        if (nameExists)
        {
            return CourtResult<CourtResponse>.Conflict("Court name already exists.");
        }

        return CourtResult<CourtResponse>.Success(new CourtResponse());
    }

    private static CourtResponse MapCourtResponse(Court court)
    {
        return new CourtResponse
        {
            Id = court.Id,
            Name = court.Name,
            CourtType = court.CourtType,
            Status = court.Status,
            CreatedAt = court.CreatedAt,
            UpdatedAt = court.UpdatedAt
        };
    }
}

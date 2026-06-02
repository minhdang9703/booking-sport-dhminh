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

        if (query.VenueId.HasValue)
        {
            courtsQuery = courtsQuery.Where(court => court.VenueId == query.VenueId.Value);
        }

        if (query.SportId.HasValue)
        {
            courtsQuery = courtsQuery.Where(court => court.SportId == query.SportId.Value);
        }

        if (query.Status.HasValue)
        {
            courtsQuery = courtsQuery.Where(court => court.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            courtsQuery = courtsQuery.Where(court => EF.Functions.ILike(court.Name, $"%{keyword}%"));
        }

        return await courtsQuery
            .OrderBy(court => court.Venue.Name)
            .ThenBy(court => court.Name)
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
        var validation = await ValidateCreateRequestAsync(request, cancellationToken);

        if (!validation.Succeeded)
        {
            return validation;
        }

        var now = DateTimeOffset.UtcNow;
        var court = new Court
        {
            Id = Guid.NewGuid(),
            VenueId = request.VenueId,
            SportId = request.SportId,
            Name = request.Name.Trim(),
            Status = request.Status,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedAt = now
        };

        dbContext.Courts.Add(court);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetCourtByIdAsync(court.Id, cancellationToken);

        return CourtResult<CourtResponse>.Success(response!);
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

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return CourtResult<CourtResponse>.BadRequest("Court name is required.");
        }

        var name = request.Name.Trim();
        var nameExists = await dbContext.Courts.AnyAsync(otherCourt =>
            otherCourt.Id != court.Id &&
            otherCourt.VenueId == court.VenueId &&
            otherCourt.DeletedAt == null &&
            otherCourt.Name == name,
            cancellationToken);

        if (nameExists)
        {
            return CourtResult<CourtResponse>.Conflict("Court name already exists in this venue.");
        }

        court.Name = name;
        court.Status = request.Status;
        court.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        court.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = await GetCourtByIdAsync(court.Id, cancellationToken);

        return CourtResult<CourtResponse>.Success(response!);
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
            .Include(court => court.Venue)
            .Include(court => court.Sport)
            .Where(court =>
                court.DeletedAt == null &&
                court.Venue.DeletedAt == null &&
                court.Sport.DeletedAt == null);
    }

    private async Task<CourtResult<CourtResponse>> ValidateCreateRequestAsync(
        CourtCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return CourtResult<CourtResponse>.BadRequest("Court name is required.");
        }

        var venueExists = await dbContext.Venues
            .AnyAsync(venue => venue.Id == request.VenueId && venue.DeletedAt == null, cancellationToken);

        if (!venueExists)
        {
            return CourtResult<CourtResponse>.BadRequest("Venue was not found.");
        }

        var sportExists = await dbContext.Sports
            .AnyAsync(sport => sport.Id == request.SportId && sport.DeletedAt == null, cancellationToken);

        if (!sportExists)
        {
            return CourtResult<CourtResponse>.BadRequest("Sport was not found.");
        }

        var name = request.Name.Trim();
        var nameExists = await dbContext.Courts.AnyAsync(court =>
            court.VenueId == request.VenueId &&
            court.DeletedAt == null &&
            court.Name == name,
            cancellationToken);

        if (nameExists)
        {
            return CourtResult<CourtResponse>.Conflict("Court name already exists in this venue.");
        }

        return CourtResult<CourtResponse>.Success(new CourtResponse());
    }

    private static CourtResponse MapCourtResponse(Court court)
    {
        return new CourtResponse
        {
            Id = court.Id,
            VenueId = court.VenueId,
            VenueName = court.Venue.Name,
            SportId = court.SportId,
            SportName = court.Sport.Name,
            Name = court.Name,
            Status = court.Status,
            Description = court.Description,
            CreatedAt = court.CreatedAt,
            UpdatedAt = court.UpdatedAt
        };
    }
}

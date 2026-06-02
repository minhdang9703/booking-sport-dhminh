using BookingSport.Api.Data;
using BookingSport.Api.DTOs.CourtSchedules;
using BookingSport.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BookingSport.Api.Services.CourtSchedules;

public class CourtScheduleService(AppDbContext dbContext) : ICourtScheduleService
{
    public async Task<IReadOnlyList<CourtScheduleResponse>> GetCourtSchedulesAsync(
        CourtScheduleQueryParameters query,
        CancellationToken cancellationToken)
    {
        var schedulesQuery = BaseScheduleQuery();

        if (query.CourtId.HasValue)
        {
            schedulesQuery = schedulesQuery.Where(schedule => schedule.CourtId == query.CourtId.Value);
        }

        if (query.DayOfWeek.HasValue)
        {
            schedulesQuery = schedulesQuery.Where(schedule => schedule.DayOfWeek == query.DayOfWeek.Value);
        }

        if (query.IsAvailable.HasValue)
        {
            schedulesQuery = schedulesQuery.Where(schedule => schedule.IsAvailable == query.IsAvailable.Value);
        }

        return await schedulesQuery
            .OrderBy(schedule => schedule.Court.Name)
            .ThenBy(schedule => schedule.DayOfWeek)
            .ThenBy(schedule => schedule.StartTime)
            .Select(schedule => MapCourtScheduleResponse(schedule))
            .ToListAsync(cancellationToken);
    }

    public async Task<CourtScheduleResponse?> GetCourtScheduleByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await BaseScheduleQuery()
            .Where(schedule => schedule.Id == id)
            .Select(schedule => MapCourtScheduleResponse(schedule))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CourtScheduleResult<CourtScheduleResponse>> CreateCourtScheduleAsync(
        CourtScheduleCreateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateScheduleAsync(request.CourtId, request.DayOfWeek, request.StartTime, request.EndTime, request.Price, null, cancellationToken);

        if (!validation.Succeeded)
        {
            return validation;
        }

        var now = DateTimeOffset.UtcNow;
        var schedule = new CourtSchedule
        {
            Id = Guid.NewGuid(),
            CourtId = request.CourtId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Price = request.Price,
            IsAvailable = request.IsAvailable,
            CreatedAt = now
        };

        dbContext.CourtSchedules.Add(schedule);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return CourtScheduleResult<CourtScheduleResponse>.Conflict("Court schedule already exists.");
        }

        var response = await GetCourtScheduleByIdAsync(schedule.Id, cancellationToken);

        return CourtScheduleResult<CourtScheduleResponse>.Success(response!);
    }

    public async Task<CourtScheduleResult<CourtScheduleResponse>> UpdateCourtScheduleAsync(
        Guid id,
        CourtScheduleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var schedule = await dbContext.CourtSchedules
            .FirstOrDefaultAsync(schedule => schedule.Id == id && schedule.DeletedAt == null, cancellationToken);

        if (schedule is null)
        {
            return CourtScheduleResult<CourtScheduleResponse>.NotFound("Court schedule was not found.");
        }

        var validation = await ValidateScheduleAsync(schedule.CourtId, request.DayOfWeek, request.StartTime, request.EndTime, request.Price, id, cancellationToken);

        if (!validation.Succeeded)
        {
            return validation;
        }

        schedule.DayOfWeek = request.DayOfWeek;
        schedule.StartTime = request.StartTime;
        schedule.EndTime = request.EndTime;
        schedule.Price = request.Price;
        schedule.IsAvailable = request.IsAvailable;
        schedule.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            return CourtScheduleResult<CourtScheduleResponse>.Conflict("Court schedule already exists.");
        }

        var response = await GetCourtScheduleByIdAsync(schedule.Id, cancellationToken);

        return CourtScheduleResult<CourtScheduleResponse>.Success(response!);
    }

    public async Task<CourtScheduleResult<bool>> DeleteCourtScheduleAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var schedule = await dbContext.CourtSchedules
            .FirstOrDefaultAsync(schedule => schedule.Id == id && schedule.DeletedAt == null, cancellationToken);

        if (schedule is null)
        {
            return CourtScheduleResult<bool>.NotFound("Court schedule was not found.");
        }

        var now = DateTimeOffset.UtcNow;
        schedule.DeletedAt = now;
        schedule.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return CourtScheduleResult<bool>.Success(true);
    }

    private async Task<CourtScheduleResult<CourtScheduleResponse>> ValidateScheduleAsync(
        Guid courtId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        decimal price,
        Guid? currentScheduleId,
        CancellationToken cancellationToken)
    {
        if (startTime >= endTime)
        {
            return CourtScheduleResult<CourtScheduleResponse>.BadRequest("StartTime must be earlier than EndTime.");
        }

        if (price < 0)
        {
            return CourtScheduleResult<CourtScheduleResponse>.BadRequest("Price must be greater than or equal to zero.");
        }

        var courtExists = await dbContext.Courts.AnyAsync(court =>
            court.Id == courtId &&
            court.DeletedAt == null &&
            court.Venue.DeletedAt == null &&
            court.Sport.DeletedAt == null,
            cancellationToken);

        if (!courtExists)
        {
            return CourtScheduleResult<CourtScheduleResponse>.BadRequest("Court was not found.");
        }

        var isDuplicate = await dbContext.CourtSchedules.AnyAsync(schedule =>
            (!currentScheduleId.HasValue || schedule.Id != currentScheduleId.Value) &&
            schedule.CourtId == courtId &&
            schedule.DayOfWeek == dayOfWeek &&
            schedule.StartTime == startTime &&
            schedule.EndTime == endTime &&
            schedule.DeletedAt == null,
            cancellationToken);

        if (isDuplicate)
        {
            return CourtScheduleResult<CourtScheduleResponse>.Conflict("Court schedule already exists.");
        }

        return CourtScheduleResult<CourtScheduleResponse>.Success(new CourtScheduleResponse());
    }

    private IQueryable<CourtSchedule> BaseScheduleQuery()
    {
        return dbContext.CourtSchedules
            .AsNoTracking()
            .Include(schedule => schedule.Court)
            .Where(schedule =>
                schedule.DeletedAt == null &&
                schedule.Court.DeletedAt == null &&
                schedule.Court.Venue.DeletedAt == null &&
                schedule.Court.Sport.DeletedAt == null);
    }

    private static CourtScheduleResponse MapCourtScheduleResponse(CourtSchedule schedule)
    {
        return new CourtScheduleResponse
        {
            Id = schedule.Id,
            CourtId = schedule.CourtId,
            CourtName = schedule.Court.Name,
            DayOfWeek = schedule.DayOfWeek,
            StartTime = schedule.StartTime,
            EndTime = schedule.EndTime,
            Price = schedule.Price,
            IsAvailable = schedule.IsAvailable,
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt
        };
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        };
    }
}

using BookingSport.Api.Data;
using BookingSport.Api.DTOs.PriceRules;
using BookingSport.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Services.PriceRules;

public class PriceRuleService(AppDbContext dbContext) : IPriceRuleService
{
    public async Task<IReadOnlyList<PriceRuleResponse>> GetPriceRulesAsync(
        PriceRuleQueryParameters query,
        CancellationToken cancellationToken)
    {
        var rulesQuery = dbContext.PriceRules.AsNoTracking().AsQueryable();

        if (query.DayOfWeek.HasValue)
        {
            rulesQuery = rulesQuery.Where(rule => rule.DayOfWeek == query.DayOfWeek.Value);
        }

        if (query.IsEnabled.HasValue)
        {
            rulesQuery = rulesQuery.Where(rule => rule.IsEnabled == query.IsEnabled.Value);
        }

        return await rulesQuery
            .OrderBy(rule => rule.DayOfWeek)
            .ThenBy(rule => rule.StartTime)
            .Select(rule => MapPriceRuleResponse(rule))
            .ToListAsync(cancellationToken);
    }

    public async Task<PriceRuleResponse?> GetPriceRuleByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.PriceRules
            .AsNoTracking()
            .Where(rule => rule.Id == id)
            .Select(rule => MapPriceRuleResponse(rule))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PriceRuleResult<PriceRuleResponse>> CreatePriceRuleAsync(
        PriceRuleCreateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await ValidateRequestAsync(null, request, cancellationToken);

        if (!validation.Succeeded)
        {
            return validation;
        }

        var rule = new PriceRule
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            HourlyPrice = request.HourlyPrice,
            IsEnabled = request.IsEnabled,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.PriceRules.Add(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return PriceRuleResult<PriceRuleResponse>.Success(MapPriceRuleResponse(rule));
    }

    public async Task<PriceRuleResult<PriceRuleResponse>> UpdatePriceRuleAsync(
        Guid id,
        PriceRuleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var rule = await dbContext.PriceRules
            .FirstOrDefaultAsync(rule => rule.Id == id, cancellationToken);

        if (rule is null)
        {
            return PriceRuleResult<PriceRuleResponse>.NotFound("Price rule was not found.");
        }

        var validation = await ValidateRequestAsync(id, request, cancellationToken);

        if (!validation.Succeeded)
        {
            return validation;
        }

        rule.Name = request.Name.Trim();
        rule.DayOfWeek = request.DayOfWeek;
        rule.StartTime = request.StartTime;
        rule.EndTime = request.EndTime;
        rule.HourlyPrice = request.HourlyPrice;
        rule.IsEnabled = request.IsEnabled;
        rule.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return PriceRuleResult<PriceRuleResponse>.Success(MapPriceRuleResponse(rule));
    }

    public async Task<PriceRuleResult<bool>> DeletePriceRuleAsync(Guid id, CancellationToken cancellationToken)
    {
        var rule = await dbContext.PriceRules
            .FirstOrDefaultAsync(rule => rule.Id == id, cancellationToken);

        if (rule is null)
        {
            return PriceRuleResult<bool>.NotFound("Price rule was not found.");
        }

        dbContext.PriceRules.Remove(rule);
        await dbContext.SaveChangesAsync(cancellationToken);

        return PriceRuleResult<bool>.Success(true);
    }

    private async Task<PriceRuleResult<PriceRuleResponse>> ValidateRequestAsync(
        Guid? id,
        PriceRuleCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return PriceRuleResult<PriceRuleResponse>.BadRequest("Name is required.");
        }

        if (request.StartTime >= request.EndTime)
        {
            return PriceRuleResult<PriceRuleResponse>.BadRequest("StartTime must be earlier than EndTime.");
        }

        if (request.HourlyPrice < 0)
        {
            return PriceRuleResult<PriceRuleResponse>.BadRequest("HourlyPrice must be greater than or equal to zero.");
        }

        var hasOverlap = await dbContext.PriceRules.AnyAsync(rule =>
            (!id.HasValue || rule.Id != id.Value) &&
            rule.IsEnabled &&
            request.IsEnabled &&
            rule.DayOfWeek == request.DayOfWeek &&
            request.StartTime < rule.EndTime &&
            request.EndTime > rule.StartTime,
            cancellationToken);

        if (hasOverlap)
        {
            return PriceRuleResult<PriceRuleResponse>.Conflict("Price rule overlaps an enabled rule in the same day.");
        }

        return PriceRuleResult<PriceRuleResponse>.Success(new PriceRuleResponse());
    }

    private static PriceRuleResponse MapPriceRuleResponse(PriceRule rule)
    {
        return new PriceRuleResponse
        {
            Id = rule.Id,
            Name = rule.Name,
            DayOfWeek = rule.DayOfWeek,
            StartTime = rule.StartTime,
            EndTime = rule.EndTime,
            HourlyPrice = rule.HourlyPrice,
            IsEnabled = rule.IsEnabled,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        };
    }
}

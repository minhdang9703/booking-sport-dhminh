using BookingSport.Api.DTOs.PriceRules;

namespace BookingSport.Api.Services.PriceRules;

public interface IPriceRuleService
{
    Task<IReadOnlyList<PriceRuleResponse>> GetPriceRulesAsync(
        PriceRuleQueryParameters query,
        CancellationToken cancellationToken);

    Task<PriceRuleResponse?> GetPriceRuleByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PriceRuleResult<PriceRuleResponse>> CreatePriceRuleAsync(
        PriceRuleCreateRequest request,
        CancellationToken cancellationToken);

    Task<PriceRuleResult<PriceRuleResponse>> UpdatePriceRuleAsync(
        Guid id,
        PriceRuleUpdateRequest request,
        CancellationToken cancellationToken);

    Task<PriceRuleResult<bool>> DeletePriceRuleAsync(Guid id, CancellationToken cancellationToken);
}

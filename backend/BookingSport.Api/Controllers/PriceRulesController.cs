using BookingSport.Api.DTOs.PriceRules;
using BookingSport.Api.Services.PriceRules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSport.Api.Controllers;

[ApiController]
[Route("api/price-rules")]
public class PriceRulesController(IPriceRuleService priceRuleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PriceRuleResponse>>> GetPriceRules(
        [FromQuery] PriceRuleQueryParameters query,
        CancellationToken cancellationToken)
    {
        var rules = await priceRuleService.GetPriceRulesAsync(query, cancellationToken);

        return Ok(rules);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PriceRuleResponse>> GetPriceRuleById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var rule = await priceRuleService.GetPriceRuleByIdAsync(id, cancellationToken);

        return rule is null ? NotFound() : Ok(rule);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<ActionResult<PriceRuleResponse>> CreatePriceRule(
        PriceRuleCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await priceRuleService.CreatePriceRuleAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetPriceRuleById), new { id = result.Value!.Id }, result.Value);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PriceRuleResponse>> UpdatePriceRule(
        Guid id,
        PriceRuleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await priceRuleService.UpdatePriceRuleAsync(id, request, cancellationToken);

        return result.Succeeded ? Ok(result.Value) : ToActionResult(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeletePriceRule(Guid id, CancellationToken cancellationToken)
    {
        var result = await priceRuleService.DeletePriceRuleAsync(id, cancellationToken);

        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    private ActionResult ToActionResult<T>(PriceRuleResult<T> result)
    {
        var body = new { message = result.Error };

        return result.Status switch
        {
            PriceRuleResultStatus.BadRequest => BadRequest(body),
            PriceRuleResultStatus.Conflict => Conflict(body),
            PriceRuleResultStatus.NotFound => NotFound(body),
            _ => StatusCode(StatusCodes.Status500InternalServerError, body)
        };
    }
}

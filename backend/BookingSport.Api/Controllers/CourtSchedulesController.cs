using BookingSport.Api.DTOs.CourtSchedules;
using BookingSport.Api.Services.CourtSchedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSport.Api.Controllers;

[ApiController]
[Route("api/court-schedules")]
public class CourtSchedulesController(ICourtScheduleService courtScheduleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourtScheduleResponse>>> GetCourtSchedules(
        [FromQuery] CourtScheduleQueryParameters query,
        CancellationToken cancellationToken)
    {
        var schedules = await courtScheduleService.GetCourtSchedulesAsync(query, cancellationToken);

        return Ok(schedules);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourtScheduleResponse>> GetCourtScheduleById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var schedule = await courtScheduleService.GetCourtScheduleByIdAsync(id, cancellationToken);

        return schedule is null ? NotFound() : Ok(schedule);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<ActionResult<CourtScheduleResponse>> CreateCourtSchedule(
        CourtScheduleCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await courtScheduleService.CreateCourtScheduleAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetCourtScheduleById),
            new { id = result.Value!.Id },
            result.Value);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CourtScheduleResponse>> UpdateCourtSchedule(
        Guid id,
        CourtScheduleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await courtScheduleService.UpdateCourtScheduleAsync(id, request, cancellationToken);

        return result.Succeeded ? Ok(result.Value) : ToActionResult(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCourtSchedule(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await courtScheduleService.DeleteCourtScheduleAsync(id, cancellationToken);

        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    private ActionResult ToActionResult<T>(CourtScheduleResult<T> result)
    {
        var body = new { message = result.Error };

        return result.Status switch
        {
            CourtScheduleResultStatus.BadRequest => BadRequest(body),
            CourtScheduleResultStatus.Conflict => Conflict(body),
            CourtScheduleResultStatus.NotFound => NotFound(body),
            _ => StatusCode(StatusCodes.Status500InternalServerError, body)
        };
    }
}

using BookingSport.Api.DTOs.Availability;
using BookingSport.Api.DTOs.Courts;
using BookingSport.Api.Services.Availability;
using BookingSport.Api.Services.Courts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSport.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourtsController(
    ICourtService courtService,
    IAvailabilityService availabilityService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CourtResponse>>> GetCourts(
        [FromQuery] CourtQueryParameters query,
        CancellationToken cancellationToken)
    {
        var courts = await courtService.GetCourtsAsync(query, cancellationToken);

        return Ok(courts);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourtResponse>> GetCourtById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var court = await courtService.GetCourtByIdAsync(id, cancellationToken);

        return court is null ? NotFound() : Ok(court);
    }

    [HttpGet("{courtId:guid}/available-schedules")]
    public async Task<ActionResult<IReadOnlyList<AvailableScheduleResponse>>> GetAvailableSchedules(
        Guid courtId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var result = await availabilityService.GetAvailableSchedulesAsync(courtId, date, cancellationToken);

        return result.Found ? Ok(result.Schedules) : NotFound();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    public async Task<ActionResult<CourtResponse>> CreateCourt(
        CourtCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await courtService.CreateCourtAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetCourtById),
            new { id = result.Value!.Id },
            result.Value);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CourtResponse>> UpdateCourt(
        Guid id,
        CourtUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await courtService.UpdateCourtAsync(id, request, cancellationToken);

        return result.Succeeded ? Ok(result.Value) : ToActionResult(result);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCourt(Guid id, CancellationToken cancellationToken)
    {
        var result = await courtService.DeleteCourtAsync(id, cancellationToken);

        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    private ActionResult ToActionResult<T>(CourtResult<T> result)
    {
        var body = new { message = result.Error };

        return result.Status switch
        {
            CourtResultStatus.BadRequest => BadRequest(body),
            CourtResultStatus.Conflict => Conflict(body),
            CourtResultStatus.NotFound => NotFound(body),
            _ => StatusCode(StatusCodes.Status500InternalServerError, body)
        };
    }
}

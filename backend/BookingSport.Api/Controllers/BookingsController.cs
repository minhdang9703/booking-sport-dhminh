using System.Security.Claims;
using BookingSport.Api.DTOs.Bookings;
using BookingSport.Api.Enums;
using BookingSport.Api.Services.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSport.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> GetBookings(
        [FromQuery] BookingQueryParameters query,
        CancellationToken cancellationToken)
    {
        var bookings = await bookingService.GetBookingsAsync(query, cancellationToken);

        return Ok(bookings);
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBooking(
        BookingCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var result = await bookingService.CreateBookingAsync(userId, request, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(
            nameof(GetBookingById),
            new { id = result.Value!.Id },
            result.Value);
    }

    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> GetMyBookings(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var bookings = await bookingService.GetMyBookingsAsync(userId, cancellationToken);

        return Ok(bookings);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> GetBookingById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole(UserRole.Admin.ToString());
        var booking = await bookingService.GetBookingByIdAsync(id, userId, isAdmin, cancellationToken);

        return booking is null ? NotFound() : Ok(booking);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<BookingResponse>> UpdateBookingStatus(
        Guid id,
        BookingUpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await bookingService.UpdateBookingStatusAsync(id, request, cancellationToken);

        return result.Succeeded ? Ok(result.Value) : ToActionResult(result);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }

    private ActionResult ToActionResult<T>(BookingResult<T> result)
    {
        var body = new { message = result.Error };

        return result.Status switch
        {
            BookingResultStatus.BadRequest => BadRequest(body),
            BookingResultStatus.Conflict => Conflict(body),
            BookingResultStatus.NotFound => NotFound(body),
            _ => StatusCode(StatusCodes.Status500InternalServerError, body)
        };
    }
}

using System.Security.Claims;
using BookingSport.Api.DTOs.Users;
using BookingSport.Api.Services.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingSport.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/[controller]")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> GetUsers(
        [FromQuery] UserQueryParameters query,
        CancellationToken cancellationToken)
    {
        var users = await userService.GetUsersAsync(query, cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await userService.GetUserByIdAsync(id, cancellationToken);

        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(
        UserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.CreateUserAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return ToActionResult(result);
        }

        return CreatedAtAction(nameof(GetUserById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(
        Guid id,
        UserUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.UpdateUserAsync(id, request, cancellationToken);

        return result.Succeeded ? Ok(result.Value) : ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
        {
            return Unauthorized();
        }

        var result = await userService.DeleteUserAsync(id, currentUserId, cancellationToken);

        return result.Succeeded ? NoContent() : ToActionResult(result);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out userId);
    }

    private ActionResult ToActionResult<T>(UserResult<T> result)
    {
        var body = new { message = result.Error };

        return result.Status switch
        {
            UserResultStatus.BadRequest => BadRequest(body),
            UserResultStatus.Conflict => Conflict(body),
            UserResultStatus.NotFound => NotFound(body),
            _ => StatusCode(StatusCodes.Status500InternalServerError, body)
        };
    }
}

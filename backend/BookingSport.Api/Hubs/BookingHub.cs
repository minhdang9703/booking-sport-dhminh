using System.Security.Claims;
using BookingSport.Api.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BookingSport.Api.Hubs;

[Authorize]
public sealed class BookingHub : Hub
{
    public const string AdminBookingsGroup = "admin-bookings";

    public async Task JoinCourt(Guid courtId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetCourtGroupName(courtId));
    }

    public async Task LeaveCourt(Guid courtId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetCourtGroupName(courtId));
    }

    public async Task JoinAdminBookings()
    {
        if (!IsAdmin())
        {
            throw new HubException("Only admins can subscribe to admin bookings.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, AdminBookingsGroup);
    }

    public static string GetCourtGroupName(Guid courtId)
    {
        return $"court-{courtId}";
    }

    public static string GetUserGroupName(Guid userId)
    {
        return $"user-{userId}";
    }

    public override async Task OnConnectedAsync()
    {
        var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userIdValue, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
        }

        await base.OnConnectedAsync();
    }

    private bool IsAdmin()
    {
        return Context.User?.IsInRole(UserRole.Admin.ToString()) == true;
    }
}

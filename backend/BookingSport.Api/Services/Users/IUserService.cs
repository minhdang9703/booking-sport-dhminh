using BookingSport.Api.DTOs.Users;

namespace BookingSport.Api.Services.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetUsersAsync(
        UserQueryParameters query,
        CancellationToken cancellationToken);

    Task<UserResponse?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<UserResult<UserResponse>> CreateUserAsync(
        UserCreateRequest request,
        CancellationToken cancellationToken);

    Task<UserResult<UserResponse>> UpdateUserAsync(
        Guid id,
        UserUpdateRequest request,
        CancellationToken cancellationToken);

    Task<UserResult<bool>> DeleteUserAsync(
        Guid id,
        Guid currentUserId,
        CancellationToken cancellationToken);
}

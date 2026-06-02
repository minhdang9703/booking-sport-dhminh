using BookingSport.Api.DTOs.Auth;

namespace BookingSport.Api.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<CurrentUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using BookingSport.Api.Data;
using BookingSport.Api.DTOs.Auth;
using BookingSport.Api.Entities;
using BookingSport.Api.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BookingSport.Api.Services.Auth;

public class AuthService(
    AppDbContext dbContext,
    IConfiguration configuration,
    PasswordHasher<User> passwordHasher,
    IAuthSettingsService authSettingsService) : IAuthService
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var phoneNumber = request.PhoneNumber.Trim();

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return AuthResult.Failure("Phone number is required.");
        }

        var phoneExists = await dbContext.Users
            .AnyAsync(user => user.PhoneNumber == phoneNumber, cancellationToken);

        if (phoneExists)
        {
            return AuthResult.Failure("Phone number is already registered.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = phoneNumber,
            Role = UserRole.Customer,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateSuccessfulAuthResultAsync(user, null, null, cancellationToken);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(user => user.Email == email && user.DeletedAt == null, cancellationToken);

        if (user is null)
        {
            return AuthResult.Failure("Invalid email or password.");
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return AuthResult.Failure("Invalid email or password.");
        }

        return await CreateSuccessfulAuthResultAsync(user, null, null, cancellationToken);
    }

    public async Task<AuthResult> RefreshAsync(
        string refreshToken,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthResult.Failure("Refresh token is required.");
        }

        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null ||
            storedToken.RevokedAt is not null ||
            storedToken.ExpiresAt <= DateTimeOffset.UtcNow ||
            storedToken.User.DeletedAt is not null)
        {
            return AuthResult.Failure("Refresh session is invalid.");
        }

        var newRefreshToken = GenerateRefreshToken();
        var lifetime = await authSettingsService.GetEffectiveLifetimeAsync(storedToken.UserId, cancellationToken);
        var newRefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(lifetime.RefreshTokenDays);
        var newRefreshTokenHash = HashRefreshToken(newRefreshToken);

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        storedToken.ReplacedByTokenHash = newRefreshTokenHash;

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = storedToken.UserId,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = newRefreshTokenExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UserAgent = TrimToMaxLength(userAgent, 500),
            IpAddress = TrimToMaxLength(ipAddress, 80)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResult.Success(
            await CreateAuthResponseAsync(storedToken.User, cancellationToken),
            newRefreshToken,
            newRefreshTokenExpiresAt);
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = HashRefreshToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || storedToken.RevokedAt is not null)
        {
            return;
        }

        storedToken.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .Where(user => user.Id == userId && user.DeletedAt == null)
            .Select(user => new CurrentUserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AuthResult> CreateSuccessfulAuthResultAsync(
        User user,
        string? userAgent,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var lifetime = await authSettingsService.GetEffectiveLifetimeAsync(user.Id, cancellationToken);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(lifetime.RefreshTokenDays);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashRefreshToken(refreshToken),
            ExpiresAt = refreshTokenExpiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UserAgent = TrimToMaxLength(userAgent, 500),
            IpAddress = TrimToMaxLength(ipAddress, 80)
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return AuthResult.Success(
            await CreateAuthResponseAsync(user, cancellationToken),
            refreshToken,
            refreshTokenExpiresAt);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(User user, CancellationToken cancellationToken)
    {
        var lifetime = await authSettingsService.GetEffectiveLifetimeAsync(user.Id, cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(lifetime.AccessTokenMinutes);

        return new AuthResponse
        {
            AccessToken = GenerateAccessToken(user, expiresAt),
            ExpiresAt = expiresAt,
            User = MapCurrentUser(user)
        };
    }

    private string GenerateAccessToken(User user, DateTimeOffset expiresAt)
    {
        var secret = configuration["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("JWT secret is not configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var role = user.Role.ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hash);
    }

    private static string? TrimToMaxLength(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static CurrentUserResponse MapCurrentUser(User user)
    {
        return new CurrentUserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role
        };
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}

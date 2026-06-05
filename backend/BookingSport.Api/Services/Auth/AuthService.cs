using System.IdentityModel.Tokens.Jwt;
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
    PasswordHasher<User> passwordHasher) : IAuthService
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

        return AuthResult.Success(CreateAuthResponse(user));
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

        return AuthResult.Success(CreateAuthResponse(user));
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

    private AuthResponse CreateAuthResponse(User user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(GetAccessTokenMinutes());

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

    private int GetAccessTokenMinutes()
    {
        return int.TryParse(configuration["Jwt:AccessTokenMinutes"], out var minutes) && minutes > 0
            ? minutes
            : 60;
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

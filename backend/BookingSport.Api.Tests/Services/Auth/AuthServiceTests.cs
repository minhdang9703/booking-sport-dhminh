using System.IdentityModel.Tokens.Jwt;
using BookingSport.Api.DTOs.Auth;
using BookingSport.Api.Entities;
using BookingSport.Api.Enums;
using BookingSport.Api.Services.Auth;
using BookingSport.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace BookingSport.Api.Tests.Services.Auth;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesCustomerAndReturnsJwt()
    {
        await using var db = await TestDb.CreateAsync();
        var service = CreateService(db.Context);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "  Nguyen Van A  ",
            Email = "  USER@Example.COM ",
            Password = "secret123",
            PhoneNumber = " 0900000001 "
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Response.Should().NotBeNull();
        result.Response!.User.Email.Should().Be("user@example.com");
        result.Response.User.FullName.Should().Be("Nguyen Van A");
        result.Response.User.PhoneNumber.Should().Be("0900000001");
        result.Response.User.Role.Should().Be(UserRole.Customer);
        result.Response.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Response.AccessToken);
        token.Claims.Should().Contain(claim => claim.Type == "email" && claim.Value == "user@example.com");
        db.Context.Users.Should().ContainSingle(user => user.Email == "user@example.com");
        db.Context.RefreshTokens.Should().ContainSingle();
    }

    [Fact]
    public async Task RegisterAsync_WhenPhoneIsBlank_ReturnsFailure()
    {
        await using var db = await TestDb.CreateAsync();
        var service = CreateService(db.Context);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "User",
            Email = "user@example.com",
            Password = "secret123",
            PhoneNumber = " "
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Phone number is required.");
    }

    [Fact]
    public async Task RegisterAsync_WhenPhoneExists_ReturnsFailure()
    {
        await using var db = await TestDb.CreateAsync();
        db.Context.Users.Add(TestData.User(phoneNumber: "0900000001"));
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "User",
            Email = "new@example.com",
            Password = "secret123",
            PhoneNumber = "0900000001"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Phone number is already registered.");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        await using var db = await TestDb.CreateAsync();
        var hasher = new PasswordHasher<User>();
        var user = TestData.User(email: "user@example.com");
        user.PasswordHash = hasher.HashPassword(user, "secret123");
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context, passwordHasher: hasher);

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = " USER@example.com ",
            Password = "secret123"
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Response!.User.Id.Should().Be(user.Id);
        result.Response.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        db.Context.RefreshTokens.Should().ContainSingle(token => token.UserId == user.Id);
    }

    [Fact]
    public async Task RefreshAsync_WithValidRefreshToken_RotatesSessionAndReturnsNewToken()
    {
        await using var db = await TestDb.CreateAsync();
        var hasher = new PasswordHasher<User>();
        var user = TestData.User(email: "user@example.com");
        user.PasswordHash = hasher.HashPassword(user, "secret123");
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context, passwordHasher: hasher);
        var login = await service.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "secret123"
        }, CancellationToken.None);

        var refreshed = await service.RefreshAsync(
            login.RefreshToken!,
            "test-agent",
            "127.0.0.1",
            CancellationToken.None);

        refreshed.Succeeded.Should().BeTrue();
        refreshed.Response!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.RefreshToken.Should().NotBe(login.RefreshToken);
        db.Context.RefreshTokens.Count().Should().Be(2);
        db.Context.RefreshTokens.Count(token => token.RevokedAt != null).Should().Be(1);
    }

    [Fact]
    public async Task LogoutAsync_WithValidRefreshToken_RevokesSession()
    {
        await using var db = await TestDb.CreateAsync();
        var hasher = new PasswordHasher<User>();
        var user = TestData.User(email: "user@example.com");
        user.PasswordHash = hasher.HashPassword(user, "secret123");
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context, passwordHasher: hasher);
        var login = await service.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "secret123"
        }, CancellationToken.None);

        await service.LogoutAsync(login.RefreshToken, CancellationToken.None);

        db.Context.RefreshTokens.Should().ContainSingle(token => token.RevokedAt != null);
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsInvalid_ReturnsFailure()
    {
        await using var db = await TestDb.CreateAsync();
        var hasher = new PasswordHasher<User>();
        var user = TestData.User(email: "user@example.com");
        user.PasswordHash = hasher.HashPassword(user, "secret123");
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context, passwordHasher: hasher);

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = "user@example.com",
            Password = "wrong"
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("Invalid email or password.");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenUserIsDeleted_ReturnsNull()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User(deletedAt: DateTimeOffset.UtcNow);
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.GetCurrentUserAsync(user.Id, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WhenJwtSecretIsMissing_ThrowsConfigurationError()
    {
        await using var db = await TestDb.CreateAsync();
        var configuration = new ConfigurationBuilder().Build();
        var service = CreateService(db.Context, configuration);

        var act = () => service.RegisterAsync(new RegisterRequest
        {
            FullName = "User",
            Email = "user@example.com",
            Password = "secret123",
            PhoneNumber = "0900000001"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("JWT secret is not configured.");
    }

    private static AuthService CreateService(
        BookingSport.Api.Data.AppDbContext dbContext,
        IConfiguration? configuration = null,
        PasswordHasher<User>? passwordHasher = null)
    {
        return new AuthService(
            dbContext,
            configuration ?? TestConfiguration.JwtConfiguration(),
            passwordHasher ?? new PasswordHasher<User>(),
            new AuthSettingsService(dbContext, configuration ?? TestConfiguration.JwtConfiguration()));
    }
}

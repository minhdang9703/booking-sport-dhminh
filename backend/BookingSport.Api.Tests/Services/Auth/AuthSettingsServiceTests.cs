using BookingSport.Api.DTOs.Auth;
using BookingSport.Api.Services.Auth;
using BookingSport.Api.Tests.TestSupport;
using FluentAssertions;

namespace BookingSport.Api.Tests.Services.Auth;

public class AuthSettingsServiceTests
{
    [Fact]
    public async Task GetGlobalSettingsAsync_WhenDatabaseIsEmpty_UsesConfigurationFallback()
    {
        await using var db = await TestDb.CreateAsync();
        var service = new AuthSettingsService(db.Context, TestConfiguration.JwtConfiguration());

        var settings = await service.GetGlobalSettingsAsync(CancellationToken.None);

        settings.AccessTokenMinutes.Should().Be(60);
        settings.RefreshTokenDays.Should().Be(14);
    }

    [Fact]
    public async Task UpdateGlobalSettingsAsync_WithValidLifetime_SavesSettings()
    {
        await using var db = await TestDb.CreateAsync();
        var service = new AuthSettingsService(db.Context, TestConfiguration.JwtConfiguration());

        var result = await service.UpdateGlobalSettingsAsync(new AuthSettingsUpdateRequest
        {
            AccessTokenMinutes = 20,
            RefreshTokenDays = 10
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        (await service.GetGlobalSettingsAsync(CancellationToken.None)).AccessTokenMinutes.Should().Be(20);
    }

    [Fact]
    public async Task UpdateGlobalSettingsAsync_WhenLifetimeIsOutsideAllowedRange_ReturnsFailure()
    {
        await using var db = await TestDb.CreateAsync();
        var service = new AuthSettingsService(db.Context, TestConfiguration.JwtConfiguration());

        var result = await service.UpdateGlobalSettingsAsync(new AuthSettingsUpdateRequest
        {
            AccessTokenMinutes = 1,
            RefreshTokenDays = 10
        }, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Access token lifetime");
    }

    [Fact]
    public async Task UserSessionSettingsAsync_WhenUserHasOverride_ReturnsUserLifetime()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();
        var service = new AuthSettingsService(db.Context, TestConfiguration.JwtConfiguration());

        var update = await service.UpdateUserSessionSettingsAsync(user.Id, new UserSessionSettingsUpdateRequest
        {
            AccessTokenMinutes = 25,
            RefreshTokenDays = 5
        }, CancellationToken.None);

        update.Succeeded.Should().BeTrue();
        var settings = await service.GetUserSessionSettingsAsync(user.Id, CancellationToken.None);
        settings.AccessTokenMinutes.Should().Be(25);
        settings.RefreshTokenDays.Should().Be(5);
    }
}

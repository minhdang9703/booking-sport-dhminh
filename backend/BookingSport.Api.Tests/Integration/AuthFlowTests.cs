using System.Net;
using System.Net.Http.Json;
using BookingSport.Api.DTOs.Auth;
using BookingSport.Api.Enums;
using BookingSport.Api.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BookingSport.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class AuthFlowTests(IntegrationTestFactory factory)
{
    [SkippableFact]
    public async Task RegisterLoginAndMeFlow_WithValidCustomer_ReturnsAuthenticatedCurrentUser()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        var client = CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "New Customer",
            Email = "NEW.CUSTOMER@test.local",
            Password = IntegrationSeedData.Password,
            PhoneNumber = "0922000001"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var registered = await registerResponse.ReadJsonAsync<AuthResponse>();
        registered.User.Email.Should().Be("new.customer@test.local");
        registered.User.Role.Should().Be(UserRole.Customer);

        var login = await client.LoginAsync("new.customer@test.local");
        client.SetBearerToken(login.AccessToken);

        var meResponse = await client.GetAsync("/api/auth/me");

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var me = await meResponse.ReadJsonAsync<CurrentUserResponse>();
        me.Email.Should().Be("new.customer@test.local");
    }

    [SkippableFact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        var client = CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task Register_WithDuplicatePhone_ReturnsConflict()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        var client = CreateClient();
        var request = new RegisterRequest
        {
            FullName = "Customer",
            Email = "customer1@test.local",
            Password = IntegrationSeedData.Password,
            PhoneNumber = "0922000002"
        };
        (await client.PostAsJsonAsync("/api/auth/register", request)).EnsureSuccessStatusCode();

        request.Email = "customer2@test.local";
        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.ReadErrorAsync()).Message.Should().Be("Phone number is already registered.");
    }

    [SkippableFact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        await factory.SeedAsync(dbContext =>
        {
            IntegrationSeedData.AddCoreData(dbContext);
            return Task.CompletedTask;
        });
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "customer.integration@test.local",
            Password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task RefreshAndLogoutFlow_WithCookie_RotatesAndRevokesSession()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        await factory.SeedAsync(dbContext =>
        {
            IntegrationSeedData.AddCoreData(dbContext);
            return Task.CompletedTask;
        });
        var client = CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "customer.integration@test.local",
            Password = IntegrationSeedData.Password
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse.Headers.GetValues("Set-Cookie")
            .Should()
            .Contain(cookie =>
                cookie.Contains("bookingSport.refresh") &&
                cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new { });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshed = await refreshResponse.ReadJsonAsync<AuthResponse>();
        refreshed.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshed.User.Email.Should().Be("customer.integration@test.local");

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new { });

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        logoutResponse.Headers.GetValues("Set-Cookie")
            .Should()
            .Contain(cookie => cookie.Contains("bookingSport.refresh") && cookie.Contains("expires="));

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/auth/refresh", new { });
        refreshAfterLogout.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [SkippableFact]
    public async Task AuthSettingsFlow_AdminCanUpdateAndCustomerIsForbidden()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        await factory.SeedAsync(dbContext =>
        {
            IntegrationSeedData.AddCoreData(dbContext);
            return Task.CompletedTask;
        });
        var client = CreateClient();
        var adminLogin = await client.LoginAsync("admin.integration@test.local");
        client.SetBearerToken(adminLogin.AccessToken);

        var updateResponse = await client.PutAsJsonAsync("/api/admin/auth-settings", new AuthSettingsUpdateRequest
        {
            AccessTokenMinutes = 20,
            RefreshTokenDays = 10
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await updateResponse.ReadJsonAsync<AuthSettingsResponse>();
        settings.AccessTokenMinutes.Should().Be(20);
        settings.RefreshTokenDays.Should().Be(10);

        var customerClient = CreateClient();
        var customerLogin = await customerClient.LoginAsync("customer.integration@test.local");
        customerClient.SetBearerToken(customerLogin.AccessToken);

        var forbiddenResponse = await customerClient.PutAsJsonAsync("/api/admin/auth-settings", new AuthSettingsUpdateRequest
        {
            AccessTokenMinutes = 15,
            RefreshTokenDays = 14
        });

        forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [SkippableFact]
    public async Task UserSessionSettingsFlow_UserCanUpdateOwnLifetime()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        await factory.SeedAsync(dbContext =>
        {
            IntegrationSeedData.AddCoreData(dbContext);
            return Task.CompletedTask;
        });
        var client = CreateClient();
        var login = await client.LoginAsync("customer.integration@test.local");
        client.SetBearerToken(login.AccessToken);

        var updateResponse = await client.PutAsJsonAsync("/api/auth/session-settings", new UserSessionSettingsUpdateRequest
        {
            AccessTokenMinutes = 25,
            RefreshTokenDays = 5
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await updateResponse.ReadJsonAsync<UserSessionSettingsResponse>();
        settings.AccessTokenMinutes.Should().Be(25);
        settings.RefreshTokenDays.Should().Be(5);

        var getResponse = await client.GetAsync("/api/auth/session-settings");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var currentSettings = await getResponse.ReadJsonAsync<UserSessionSettingsResponse>();
        currentSettings.AccessTokenMinutes.Should().Be(25);
        currentSettings.RefreshTokenDays.Should().Be(5);
    }

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}

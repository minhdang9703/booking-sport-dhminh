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

    private HttpClient CreateClient()
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }
}

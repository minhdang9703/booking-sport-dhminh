using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BookingSport.Api.DTOs.Auth;
using BookingSport.Api.DTOs.Bookings;

namespace BookingSport.Api.Tests.Integration.Support;

public static class ApiClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task<TResponse> ReadJsonAsync<TResponse>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions);

        return value ?? throw new InvalidOperationException("Response body was empty.");
    }

    public static async Task<ErrorResponse> ReadErrorAsync(this HttpResponseMessage response)
    {
        return await response.ReadJsonAsync<ErrorResponse>();
    }

    public static void SetBearerToken(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    public static async Task<AuthResponse> LoginAsync(
        this HttpClient client,
        string email,
        string password = IntegrationSeedData.Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        }, JsonOptions);

        response.EnsureSuccessStatusCode();

        return await response.ReadJsonAsync<AuthResponse>();
    }

    public static async Task<BookingResponse> CreateBookingAsync(
        this HttpClient client,
        BookingCreateRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/bookings", request, JsonOptions);
        response.EnsureSuccessStatusCode();

        return await response.ReadJsonAsync<BookingResponse>();
    }
}

public sealed class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}

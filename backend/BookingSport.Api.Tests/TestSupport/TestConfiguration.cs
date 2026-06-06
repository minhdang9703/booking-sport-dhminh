using Microsoft.Extensions.Configuration;

namespace BookingSport.Api.Tests.TestSupport;

public static class TestConfiguration
{
    public static IConfiguration JwtConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "booking-sport-unit-test-secret-with-enough-length",
                ["Jwt:Issuer"] = "BookingSport.Tests",
                ["Jwt:Audience"] = "BookingSport.Tests",
                ["Jwt:AccessTokenMinutes"] = "120"
            })
            .Build();
    }
}

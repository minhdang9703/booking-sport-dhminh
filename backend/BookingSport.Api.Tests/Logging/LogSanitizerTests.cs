using BookingSport.Api.Logging;
using FluentAssertions;

namespace BookingSport.Api.Tests.Logging;

public class LogSanitizerTests
{
    [Fact]
    public void SanitizeText_WhenJsonContainsSensitiveFields_MasksValues()
    {
        var sanitizer = new LogSanitizer();

        var result = sanitizer.SanitizeText("""
        {
            "email": "customer@example.com",
            "password": "secret",
            "nested": {
                "accessToken": "token-value"
            }
        }
        """);

        result.Should().Contain("\"email\":\"customer@example.com\"");
        result.Should().Contain("\"password\":\"***\"");
        result.Should().Contain("\"accessToken\":\"***\"");
        result.Should().NotContain("secret");
        result.Should().NotContain("token-value");
    }

    [Fact]
    public void SanitizeText_WhenPlainQueryStringContainsSensitiveFields_MasksValues()
    {
        var sanitizer = new LogSanitizer();

        var result = sanitizer.SanitizeText("?email=customer@example.com&password=secret&token=abc");

        result.Should().Contain("email=customer@example.com");
        result.Should().Contain("password=***");
        result.Should().Contain("token=***");
        result.Should().NotContain("secret");
        result.Should().NotContain("abc");
    }
}

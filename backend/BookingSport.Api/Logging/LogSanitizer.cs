using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BookingSport.Api.Logging;

public sealed class LogSanitizer : ILogSanitizer
{
    private const string Mask = "***";

    private static readonly HashSet<string> SensitiveNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "currentPassword",
        "newPassword",
        "confirmPassword",
        "accessToken",
        "refreshToken",
        "token",
        "authorization",
        "cookie",
        "smtpPassword",
        "secret",
        "clientSecret"
    };

    public string SanitizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            var node = JsonNode.Parse(value);

            if (node is null)
            {
                return string.Empty;
            }

            SanitizeNode(node);

            return node.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
        catch (JsonException)
        {
            var sanitizedValue = SanitizePlainText(value);

            return sanitizedValue.Length > 512 ? $"{sanitizedValue[..512]}..." : sanitizedValue;
        }
    }

    private static string SanitizePlainText(string value)
    {
        foreach (var sensitiveName in SensitiveNames)
        {
            value = Regex.Replace(
                value,
                $"(?i)([?&\\s]?{Regex.Escape(sensitiveName)}=)([^&\\s]+)",
                $"$1{Mask}");
        }

        return value;
    }

    private static void SanitizeNode(JsonNode node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (SensitiveNames.Contains(property.Key))
                {
                    jsonObject[property.Key] = Mask;
                    continue;
                }

                if (property.Value is not null)
                {
                    SanitizeNode(property.Value);
                }
            }
        }

        if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray)
            {
                if (item is not null)
                {
                    SanitizeNode(item);
                }
            }
        }
    }
}

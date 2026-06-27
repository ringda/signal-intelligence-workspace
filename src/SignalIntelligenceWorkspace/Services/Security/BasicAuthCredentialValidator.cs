using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace SignalIntelligenceWorkspace.Services.Security;

public static class BasicAuthCredentialValidator
{
    public static bool IsAuthorized(string? authorizationHeader, BasicAuthOptions options)
    {
        if (!options.IsConfigured ||
            string.IsNullOrWhiteSpace(authorizationHeader) ||
            !AuthenticationHeaderValue.TryParse(authorizationHeader, out var header) ||
            !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(header.Parameter))
        {
            return false;
        }

        string decoded;
        try
        {
            decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
        }
        catch (FormatException)
        {
            return false;
        }

        var separatorIndex = decoded.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            return false;
        }

        var username = decoded[..separatorIndex];
        var password = decoded[(separatorIndex + 1)..];

        return FixedTimeEquals(username, options.Username!) &&
            FixedTimeEquals(password, options.Password!);
    }

    private static bool FixedTimeEquals(string actual, string expected)
    {
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(actual));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}

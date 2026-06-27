using System.Text;
using SignalIntelligenceWorkspace.Services.Security;

namespace SignalIntelligenceWorkspace.Tests;

public class BasicAuthCredentialValidatorTests
{
    private static readonly BasicAuthOptions Options = new()
    {
        Username = "dashboard",
        Password = "secret-password",
        Realm = "Test Realm",
    };

    [Fact]
    public void IsAuthorized_AllowsExactCredentials()
    {
        var header = BuildHeader("dashboard", "secret-password");

        Assert.True(BasicAuthCredentialValidator.IsAuthorized(header, Options));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer abc")]
    [InlineData("Basic not-base64")]
    public void IsAuthorized_RejectsMissingOrMalformedHeader(string? header)
    {
        Assert.False(BasicAuthCredentialValidator.IsAuthorized(header, Options));
    }

    [Theory]
    [InlineData("dashboard", "wrong-password")]
    [InlineData("wrong-user", "secret-password")]
    [InlineData("wrong-user", "wrong-password")]
    public void IsAuthorized_RejectsWrongCredentials(string username, string password)
    {
        var header = BuildHeader(username, password);

        Assert.False(BasicAuthCredentialValidator.IsAuthorized(header, Options));
    }

    [Fact]
    public void IsAuthorized_RejectsUnconfiguredOptions()
    {
        var header = BuildHeader("dashboard", "secret-password");
        var options = new BasicAuthOptions();

        Assert.False(BasicAuthCredentialValidator.IsAuthorized(header, options));
    }

    private static string BuildHeader(string username, string password)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));

        return $"Basic {credentials}";
    }
}

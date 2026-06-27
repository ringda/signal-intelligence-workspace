using Microsoft.Extensions.Configuration;

namespace SignalIntelligenceWorkspace.Services.Security;

public sealed class BasicAuthOptions
{
    public const string SectionName = "BasicAuth";
    public const string DefaultRealm = "Signal Intelligence Workspace";

    public string? Username { get; init; }
    public string? Password { get; init; }
    public string Realm { get; init; } = DefaultRealm;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Username) &&
        !string.IsNullOrEmpty(Password);

    public static BasicAuthOptions FromConfiguration(IConfiguration configuration)
    {
        var realm = configuration[$"{SectionName}:Realm"];

        return new BasicAuthOptions
        {
            Username = configuration[$"{SectionName}:Username"],
            Password = configuration[$"{SectionName}:Password"],
            Realm = string.IsNullOrWhiteSpace(realm) ? DefaultRealm : realm,
        };
    }
}

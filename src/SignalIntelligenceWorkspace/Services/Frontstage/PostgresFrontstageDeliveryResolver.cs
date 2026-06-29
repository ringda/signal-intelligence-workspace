using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace SignalIntelligenceWorkspace.Services.Frontstage;

public sealed partial class PostgresFrontstageDeliveryResolver : IFrontstageDeliveryResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IConfiguration configuration;
    private readonly ILogger<PostgresFrontstageDeliveryResolver> logger;
    private readonly string connectionStringKey;
    private readonly string schemaName;
    private readonly string tokenPepper;

    public PostgresFrontstageDeliveryResolver(
        IConfiguration configuration,
        IOptions<FrontstageDeliveryOptions> options,
        ILogger<PostgresFrontstageDeliveryResolver> logger)
    {
        this.configuration = configuration;
        this.logger = logger;

        var configuredOptions = options.Value;
        connectionStringKey = string.IsNullOrWhiteSpace(configuredOptions.ConnectionStringKey)
            ? "Cockpit:ConnectionString"
            : configuredOptions.ConnectionStringKey;
        schemaName = ValidateIdentifier(configuredOptions.SchemaName, nameof(configuredOptions.SchemaName));

        tokenPepper = configuration["TOKEN_PEPPER"]
            ?? configuredOptions.TokenPepper
            ?? "frontstage-dev-pepper-20260626";
    }

    public async Task<FrontstageDeliveryContext?> ResolveAsync(
        FrontstageDeliveryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return null;
        }

        var connectionString = configuration[connectionStringKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning(
                "Frontstage delivery requested, but {ConnectionStringKey} is not configured.",
                connectionStringKey);
            return null;
        }

        var tokenHash = ComputeTokenHash(request.Token, tokenPepper);

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var context = await ResolveContextAsync(connection, tokenHash, cancellationToken);
            if (context is not null && request.LogVisit)
            {
                await TryLogVisitAsync(connection, context, request, cancellationToken);
            }

            return context;
        }
        catch (Exception exception) when (exception is NpgsqlException or JsonException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Frontstage delivery fell back to the generic public page.");
            return null;
        }
    }

    internal static string ComputeTokenHash(string token, string pepper)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{token}.{pepper}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    internal static string BuildRedactedPath(string language)
    {
        return NormalizeLanguage(language) switch
        {
            "zh" => "/r/[redacted-token]?lang=zh",
            _ => "/r/[redacted-token]?lang=en",
        };
    }

    internal static string NormalizeLanguage(string language)
    {
        return string.Equals(language, "zh", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(language, "zh-Hant", StringComparison.OrdinalIgnoreCase)
            ? "zh"
            : "en";
    }

    internal static string? GetReferrerDomain(string? referrer)
    {
        if (string.IsNullOrWhiteSpace(referrer) ||
            !Uri.TryCreate(referrer, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(uri.Host) ? null : uri.Host.ToLowerInvariant();
    }

    internal static string GetUserAgentFamily(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "unknown";
        }

        var text = userAgent.ToLowerInvariant();
        if (text.Contains("bot", StringComparison.Ordinal) ||
            text.Contains("crawler", StringComparison.Ordinal) ||
            text.Contains("spider", StringComparison.Ordinal))
        {
            return "bot";
        }

        if (text.Contains("edg/", StringComparison.Ordinal))
        {
            return "edge";
        }

        if (text.Contains("chrome/", StringComparison.Ordinal))
        {
            return "chrome";
        }

        if (text.Contains("safari/", StringComparison.Ordinal))
        {
            return "safari";
        }

        if (text.Contains("firefox/", StringComparison.Ordinal))
        {
            return "firefox";
        }

        return "other";
    }

    private async Task<FrontstageDeliveryContext?> ResolveContextAsync(
        NpgsqlConnection connection,
        string tokenHash,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            SELECT
                at.token_hash,
                at.audience_key,
                at.role_lens_key,
                at.page_version,
                at.content_variant_id,
                cv.public_copy_json::text
            FROM {Quote(schemaName)}.audience_tokens at
            INNER JOIN {Quote(schemaName)}.content_variants cv
                ON cv.variant_id = at.content_variant_id
            WHERE at.token_hash = @tokenHash
                AND at.status = 'active'
                AND cv.status = 'approved'
                AND (at.expires_at IS NULL OR at.expires_at > now())
            LIMIT 1
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tokenHash", tokenHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var publicCopy = JsonSerializer.Deserialize<Dictionary<string, FrontstageDeliveryCopy>>(
            reader.GetString(5),
            JsonOptions) ?? [];

        return new FrontstageDeliveryContext(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            publicCopy);
    }

    private async Task TryLogVisitAsync(
        NpgsqlConnection connection,
        FrontstageDeliveryContext context,
        FrontstageDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var sql = $"""
                INSERT INTO {Quote(schemaName)}.visit_events
                    (token_hash, page_version, event_type, path, referrer_domain, user_agent_family)
                VALUES
                    (@tokenHash, @pageVersion, 'page_view', @path, @referrerDomain, @userAgentFamily)
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("tokenHash", context.TokenHash);
            command.Parameters.AddWithValue("pageVersion", context.PageVersion);
            command.Parameters.AddWithValue("path", BuildRedactedPath(request.Language));
            command.Parameters.AddWithValue("referrerDomain", (object?)GetReferrerDomain(request.Referrer) ?? DBNull.Value);
            command.Parameters.AddWithValue("userAgentFamily", GetUserAgentFamily(request.UserAgent));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (NpgsqlException exception)
        {
            logger.LogWarning(exception, "Frontstage visit logging failed; personalized page rendering continued.");
        }
    }

    private static string ValidateIdentifier(string value, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value) || !IdentifierRegex().IsMatch(value))
        {
            throw new InvalidOperationException($"FrontstageDelivery:{optionName} must be a valid PostgreSQL identifier.");
        }

        return value;
    }

    private static string Quote(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    [GeneratedRegex("^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex IdentifierRegex();
}

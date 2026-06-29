using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace SignalIntelligenceWorkspace.Services.Frontstage;

public sealed partial class PostgresFrontstageDeliveryResolver : IFrontstageDeliveryResolver
{
    private const string AnonymousPageVersion = "frontstage-public-v1";

    private static readonly HashSet<string> AllowedSectionKeys = new(StringComparer.Ordinal)
    {
        "hero",
        "workflow",
        "reliability",
        "marketing-fit",
        "conversation",
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

    public async Task<FrontstageTokenContext?> ResolveAsync(
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

            var context = await ResolveTokenContextAsync(connection, tokenHash, cancellationToken);
            if (context is not null && request.LogVisit)
            {
                await TryLogVisitAsync(connection, context, request, cancellationToken);
            }

            return context;
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Frontstage delivery fell back to the generic public page.");
            return null;
        }
    }

    public async Task<bool> LogAnonymousPageViewAsync(
        FrontstageAnonymousPageViewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.LogVisit)
        {
            return false;
        }

        var connectionString = configuration[connectionStringKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning(
                "Anonymous frontstage page view requested, but {ConnectionStringKey} is not configured.",
                connectionStringKey);
            return false;
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await LogAnonymousEventAsync(
                connection,
                "page_view",
                request.PagePath,
                request.Language,
                request.Referrer,
                request.UserAgent,
                cancellationToken);

            return true;
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Anonymous frontstage page view logging failed.");
            return false;
        }
    }

    public async Task<bool> LogSectionViewAsync(
        FrontstageSectionViewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) ||
            !TryNormalizeSectionKey(request.SectionKey, out var sectionKey))
        {
            return false;
        }

        var connectionString = configuration[connectionStringKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning(
                "Frontstage section view requested, but {ConnectionStringKey} is not configured.",
                connectionStringKey);
            return false;
        }

        var tokenHash = ComputeTokenHash(request.Token, tokenPepper);

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var context = await ResolveTokenContextAsync(connection, tokenHash, cancellationToken);
            if (context is null)
            {
                return false;
            }

            await LogEventAsync(
                connection,
                context,
                "section_view",
                request.Language,
                sectionKey,
                request.Referrer,
                request.UserAgent,
                cancellationToken);

            return true;
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Frontstage section view logging failed.");
            return false;
        }
    }

    public async Task<bool> LogClickAsync(
        FrontstageClickRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) ||
            !TryNormalizeClickEventKey(request.EventKey, out var eventKey))
        {
            return false;
        }

        var connectionString = configuration[connectionStringKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning(
                "Frontstage click event requested, but {ConnectionStringKey} is not configured.",
                connectionStringKey);
            return false;
        }

        var tokenHash = ComputeTokenHash(request.Token, tokenPepper);

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var context = await ResolveTokenContextAsync(connection, tokenHash, cancellationToken);
            if (context is null)
            {
                return false;
            }

            await LogEventAsync(
                connection,
                context,
                "click_event",
                request.Language,
                $"click:{eventKey}",
                request.Referrer,
                request.UserAgent,
                cancellationToken);

            return true;
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Frontstage click event logging failed.");
            return false;
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

    internal static string BuildAnonymousPath(string pagePath, string language)
    {
        var normalizedPath = pagePath switch
        {
            "/home" => "/home",
            _ => "/",
        };

        return NormalizeLanguage(language) switch
        {
            "zh" => $"{normalizedPath}?lang=zh",
            _ => $"{normalizedPath}?lang=en",
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

    internal static bool TryNormalizeSectionKey(string sectionKey, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(sectionKey))
        {
            return false;
        }

        var candidate = sectionKey.Trim().ToLowerInvariant();
        if (!AllowedSectionKeys.Contains(candidate))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    internal static bool TryNormalizeClickEventKey(string eventKey, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(eventKey))
        {
            return false;
        }

        var candidate = eventKey.Trim().ToLowerInvariant();
        if (candidate.Length is < 2 or > 48 ||
            !ClickEventKeyRegex().IsMatch(candidate))
        {
            return false;
        }

        normalized = candidate;
        return true;
    }

    private async Task<FrontstageTokenContext?> ResolveTokenContextAsync(
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
                at.hubspot_task_id,
                at.outreach_attempt_key,
                at.repo_jd_folder
            FROM {Quote(schemaName)}.audience_tokens at
            WHERE at.token_hash = @tokenHash
                AND at.status = 'active'
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

        return new FrontstageTokenContext(
            reader.GetString(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetString(6),
            reader.IsDBNull(7) ? null : reader.GetString(7));
    }

    private async Task TryLogVisitAsync(
        NpgsqlConnection connection,
        FrontstageTokenContext context,
        FrontstageDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await LogEventAsync(
                connection,
                context,
                "page_view",
                request.Language,
                sectionKey: null,
                request.Referrer,
                request.UserAgent,
                cancellationToken);
        }
        catch (NpgsqlException exception)
        {
            logger.LogWarning(exception, "Frontstage visit logging failed; personalized page rendering continued.");
        }
    }

    private async Task LogEventAsync(
        NpgsqlConnection connection,
        FrontstageTokenContext context,
        string eventType,
        string language,
        string? sectionKey,
        string? referrer,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            INSERT INTO {Quote(schemaName)}.visit_events
                (token_hash, page_version, event_type, path, referrer_domain, user_agent_family)
            VALUES
                (@tokenHash, @pageVersion, @eventType, @path, @referrerDomain, @userAgentFamily)
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("tokenHash", context.TokenHash);
        command.Parameters.AddWithValue("pageVersion", context.PageVersion);
        command.Parameters.AddWithValue("eventType", eventType);
        command.Parameters.AddWithValue("path", sectionKey is null
            ? BuildRedactedPath(language)
            : $"{BuildRedactedPath(language)}#{sectionKey}");
        command.Parameters.AddWithValue("referrerDomain", (object?)GetReferrerDomain(referrer) ?? DBNull.Value);
        command.Parameters.AddWithValue("userAgentFamily", GetUserAgentFamily(userAgent));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task LogAnonymousEventAsync(
        NpgsqlConnection connection,
        string eventType,
        string pagePath,
        string language,
        string? referrer,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        var sql = $"""
            INSERT INTO {Quote(schemaName)}.visit_events
                (token_hash, page_version, event_type, path, referrer_domain, user_agent_family, hubspot_sync_status)
            VALUES
                (NULL, @pageVersion, @eventType, @path, @referrerDomain, @userAgentFamily, 'skipped')
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("pageVersion", AnonymousPageVersion);
        command.Parameters.AddWithValue("eventType", eventType);
        command.Parameters.AddWithValue("path", BuildAnonymousPath(pagePath, language));
        command.Parameters.AddWithValue("referrerDomain", (object?)GetReferrerDomain(referrer) ?? DBNull.Value);
        command.Parameters.AddWithValue("userAgentFamily", GetUserAgentFamily(userAgent));
        await command.ExecuteNonQueryAsync(cancellationToken);
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

    [GeneratedRegex("^[a-z0-9][a-z0-9-]*$")]
    private static partial Regex ClickEventKeyRegex();
}

using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace SignalIntelligenceWorkspace.Services.PublicCadence;

public sealed partial class PostgresPublicOperatingCadenceReader : IPublicOperatingCadenceReader
{
    private static readonly HashSet<string> AllowedActivityLevels = new(StringComparer.Ordinal)
    {
        "none",
        "present",
        "active",
        "heavy",
    };

    private readonly IConfiguration configuration;
    private readonly ILogger<PostgresPublicOperatingCadenceReader> logger;
    private readonly TimeProvider timeProvider;
    private readonly string connectionStringKey;
    private readonly string schemaName;
    private readonly string viewName;

    public PostgresPublicOperatingCadenceReader(
        IConfiguration configuration,
        IOptions<PublicOperatingCadenceOptions> options,
        ILogger<PostgresPublicOperatingCadenceReader> logger,
        TimeProvider timeProvider)
    {
        this.configuration = configuration;
        this.logger = logger;
        this.timeProvider = timeProvider;

        var configuredOptions = options.Value;
        connectionStringKey = string.IsNullOrWhiteSpace(configuredOptions.ConnectionStringKey)
            ? "Cockpit:ConnectionString"
            : configuredOptions.ConnectionStringKey;
        schemaName = ValidateIdentifier(configuredOptions.SchemaName, nameof(configuredOptions.SchemaName));
        viewName = ValidateIdentifier(configuredOptions.ViewName, nameof(configuredOptions.ViewName));
    }

    public async Task<PublicOperatingCadenceSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = configuration[connectionStringKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning(
                "Public operating cadence requested, but {ConnectionStringKey} is not configured.",
                connectionStringKey);
            return CreateFallback();
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"""
                SELECT
                    row_order,
                    step_key,
                    step_label,
                    day_position,
                    display_day_key,
                    weekday_label,
                    activity_level,
                    generated_at,
                    display_timezone
                FROM {Quote(schemaName)}.{Quote(viewName)}
                ORDER BY row_order, day_position
                """;

            await using var command = new NpgsqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<CadenceRecord>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var activityLevel = reader.GetString(6);
                rows.Add(new CadenceRecord(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetString(4),
                    reader.GetString(5),
                    AllowedActivityLevels.Contains(activityLevel) ? activityLevel : "none",
                    new DateTimeOffset(DateTime.SpecifyKind(reader.GetDateTime(7), DateTimeKind.Utc)),
                    reader.GetString(8)));
            }

            if (rows.Count == 0)
            {
                logger.LogWarning("Public operating cadence view returned no rows.");
                return CreateFallback();
            }

            return ToSnapshot(rows);
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Public operating cadence fell back to static cadence.");
            return CreateFallback();
        }
    }

    private PublicOperatingCadenceSnapshot CreateFallback()
    {
        return PublicOperatingCadenceSnapshot.Fallback(timeProvider.GetUtcNow());
    }

    private static PublicOperatingCadenceSnapshot ToSnapshot(IReadOnlyList<CadenceRecord> records)
    {
        var days = records
            .GroupBy(record => record.DayPosition)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var first = group.First();
                return new PublicOperatingCadenceDay(
                    first.DayPosition,
                    first.DisplayDayKey,
                    first.WeekdayLabel);
            })
            .ToArray();

        var rows = records
            .GroupBy(record => new { record.RowOrder, record.StepKey, record.StepLabel })
            .OrderBy(group => group.Key.RowOrder)
            .Select(group => new PublicOperatingCadenceRow(
                group.Key.StepKey,
                group.Key.StepLabel,
                true,
                group.OrderBy(record => record.DayPosition)
                    .Select(record => new PublicOperatingCadenceCell(
                        record.DayPosition,
                        record.DisplayDayKey,
                        record.WeekdayLabel,
                        record.ActivityLevel))
                    .ToArray()))
            .ToArray();

        var generatedAt = records.Max(record => record.GeneratedAt);
        var displayTimezone = records
            .Select(record => record.DisplayTimezone)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
            ?? "Asia/Taipei";

        return new PublicOperatingCadenceSnapshot(days, rows, generatedAt, displayTimezone);
    }

    private static string ValidateIdentifier(string value, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value) || !IdentifierRegex().IsMatch(value))
        {
            throw new InvalidOperationException($"PublicOperatingCadence:{optionName} must be a valid PostgreSQL identifier.");
        }

        return value;
    }

    private static string Quote(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    [GeneratedRegex("^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex IdentifierRegex();

    private sealed record CadenceRecord(
        int RowOrder,
        string StepKey,
        string StepLabel,
        int DayPosition,
        string DisplayDayKey,
        string WeekdayLabel,
        string ActivityLevel,
        DateTimeOffset GeneratedAt,
        string DisplayTimezone);
}

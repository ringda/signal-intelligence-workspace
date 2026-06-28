using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public sealed partial class PostgresPublicFeedbackWriter : IPublicFeedbackWriter
{
    private readonly string connectionString;
    private readonly string schemaName;
    private readonly string tableName;

    public PostgresPublicFeedbackWriter(
        IConfiguration configuration,
        IOptions<PublicFeedbackOptions> options)
    {
        var configuredOptions = options.Value;
        var connectionStringKey = string.IsNullOrWhiteSpace(configuredOptions.ConnectionStringKey)
            ? "Cockpit:ConnectionString"
            : configuredOptions.ConnectionStringKey;

        connectionString = configuration[connectionStringKey]
            ?? throw new InvalidOperationException($"{connectionStringKey} is not configured.");
        schemaName = ValidateIdentifier(configuredOptions.SchemaName, nameof(configuredOptions.SchemaName));
        tableName = ValidateIdentifier(configuredOptions.TableName, nameof(configuredOptions.TableName));
    }

    public async Task WriteAsync(PublicFeedbackRecord record, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"""
            INSERT INTO {Quote(schemaName)}.{Quote(tableName)}
                (id, submitted_at, feedback_type, message, page_path)
            VALUES
                (@id, @submittedAt, @feedbackType, @message, @pagePath)
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", record.Id);
        command.Parameters.AddWithValue("submittedAt", record.SubmittedAt);
        command.Parameters.AddWithValue("feedbackType", record.FeedbackType);
        command.Parameters.AddWithValue("message", record.Message);
        command.Parameters.AddWithValue("pagePath", record.PagePath);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string ValidateIdentifier(string value, string optionName)
    {
        if (string.IsNullOrWhiteSpace(value) || !IdentifierRegex().IsMatch(value))
        {
            throw new InvalidOperationException($"PublicFeedback:{optionName} must be a valid PostgreSQL identifier.");
        }

        return value;
    }

    internal static string ValidateIdentifierForSql(string value, string optionName)
    {
        return ValidateIdentifier(value, optionName);
    }

    internal static string QuoteIdentifier(string identifier)
    {
        return Quote(identifier);
    }

    private static string Quote(string identifier)
    {
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }

    [GeneratedRegex("^[a-zA-Z_][a-zA-Z0-9_]*$")]
    private static partial Regex IdentifierRegex();
}

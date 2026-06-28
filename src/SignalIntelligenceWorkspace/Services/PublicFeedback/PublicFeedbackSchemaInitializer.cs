using Microsoft.Extensions.Options;
using Npgsql;

namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public sealed class PublicFeedbackSchemaInitializer
{
    private readonly string connectionString;
    private readonly string schemaName;
    private readonly string tableName;

    public PublicFeedbackSchemaInitializer(
        IConfiguration configuration,
        IOptions<PublicFeedbackOptions> options)
    {
        var configuredOptions = options.Value;
        var connectionStringKey = string.IsNullOrWhiteSpace(configuredOptions.ConnectionStringKey)
            ? "Cockpit:ConnectionString"
            : configuredOptions.ConnectionStringKey;

        connectionString = configuration[connectionStringKey]
            ?? throw new InvalidOperationException($"{connectionStringKey} is not configured.");
        schemaName = PostgresPublicFeedbackWriter.ValidateIdentifierForSql(
            configuredOptions.SchemaName,
            nameof(configuredOptions.SchemaName));
        tableName = PostgresPublicFeedbackWriter.ValidateIdentifierForSql(
            configuredOptions.TableName,
            nameof(configuredOptions.TableName));
    }

    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(BuildSchemaSql(schemaName, tableName), connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static string BuildSchemaSql(string schemaName, string tableName)
    {
        schemaName = PostgresPublicFeedbackWriter.ValidateIdentifierForSql(schemaName, nameof(schemaName));
        tableName = PostgresPublicFeedbackWriter.ValidateIdentifierForSql(tableName, nameof(tableName));

        var quotedSchema = PostgresPublicFeedbackWriter.QuoteIdentifier(schemaName);
        var quotedTable = PostgresPublicFeedbackWriter.QuoteIdentifier(tableName);
        var fullTableName = $"{quotedSchema}.{quotedTable}";
        var submittedAtIndex = PostgresPublicFeedbackWriter.QuoteIdentifier($"ix_{tableName}_submitted_at");
        var typeSubmittedAtIndex = PostgresPublicFeedbackWriter.QuoteIdentifier($"ix_{tableName}_type_submitted_at");

        return $"""
            CREATE SCHEMA IF NOT EXISTS {quotedSchema};

            CREATE TABLE IF NOT EXISTS {fullTableName} (
                id text PRIMARY KEY,
                submitted_at timestamptz NOT NULL,
                feedback_type text NOT NULL,
                message text NOT NULL,
                page_path text NOT NULL,
                created_at timestamptz NOT NULL DEFAULT now()
            );

            CREATE INDEX IF NOT EXISTS {submittedAtIndex}
                ON {fullTableName} (submitted_at DESC);

            CREATE INDEX IF NOT EXISTS {typeSubmittedAtIndex}
                ON {fullTableName} (feedback_type, submitted_at DESC);
            """;
    }
}

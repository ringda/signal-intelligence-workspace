using Npgsql;
using SignalIntelligenceWorkspace.Models.Cockpit;

namespace SignalIntelligenceWorkspace.Services.Cockpit;

public sealed class CockpitDataService(IConfiguration configuration)
{
    private readonly string _connectionString = configuration["Cockpit:ConnectionString"]
        ?? throw new InvalidOperationException("Cockpit:ConnectionString is not configured.");

    public async Task<CockpitDailyMetrics?> GetDailyMetricsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT jd_scanned_24h,
                   jd_scanned_target,
                   jd_correct_pool_24h,
                   jd_scanned_week,
                   applications_applied_today,
                   applications_applied_target,
                   applications_applied_week,
                   applications_applied_week_target,
                   jd_scanned_today,
                   jd_correct_pool_today,
                   applications_role_briefed_today,
                   applications_resume_customized_today,
                   applications_networking_started_today,
                   local_today,
                   local_week_start,
                   metric_timezone
            FROM core.cockpit_daily_metrics
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CockpitDailyMetrics
        {
            JdScanned24h = reader.GetInt64(reader.GetOrdinal("jd_scanned_24h")),
            JdScannedTarget = reader.GetInt32(reader.GetOrdinal("jd_scanned_target")),
            JdCorrectPool24h = reader.GetInt64(reader.GetOrdinal("jd_correct_pool_24h")),
            JdScannedWeek = reader.GetInt64(reader.GetOrdinal("jd_scanned_week")),
            ApplicationsAppliedToday = reader.GetInt64(reader.GetOrdinal("applications_applied_today")),
            ApplicationsAppliedTarget = reader.GetInt32(reader.GetOrdinal("applications_applied_target")),
            ApplicationsAppliedWeek = reader.GetInt64(reader.GetOrdinal("applications_applied_week")),
            ApplicationsAppliedWeekTarget = reader.GetInt32(reader.GetOrdinal("applications_applied_week_target")),
            JdScannedToday = reader.GetInt64(reader.GetOrdinal("jd_scanned_today")),
            JdCorrectPoolToday = reader.GetInt64(reader.GetOrdinal("jd_correct_pool_today")),
            ApplicationsRoleBriefedToday = reader.GetInt64(reader.GetOrdinal("applications_role_briefed_today")),
            ApplicationsResumeCustomizedToday = reader.GetInt64(reader.GetOrdinal("applications_resume_customized_today")),
            ApplicationsNetworkingStartedToday = reader.GetInt64(reader.GetOrdinal("applications_networking_started_today")),
            LocalToday = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("local_today")),
            LocalWeekStart = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("local_week_start")),
            MetricTimezone = reader.GetString(reader.GetOrdinal("metric_timezone")),
        };
    }

    public async Task<List<CockpitDailySeries>> GetDailySeriesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT day,
                   jd_scanned,
                   jd_scanned_target,
                   jd_correct_pool,
                   applications_applied,
                   applications_applied_target
            FROM core.cockpit_daily_series
            ORDER BY day
            """;

        var rows = new List<CockpitDailySeries>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new CockpitDailySeries
            {
                Day = reader.GetDateTime(reader.GetOrdinal("day")),
                JdScanned = reader.GetInt32(reader.GetOrdinal("jd_scanned")),
                JdScannedTarget = reader.GetInt32(reader.GetOrdinal("jd_scanned_target")),
                JdCorrectPool = reader.GetInt32(reader.GetOrdinal("jd_correct_pool")),
                ApplicationsApplied = reader.GetInt32(reader.GetOrdinal("applications_applied")),
                ApplicationsAppliedTarget = reader.GetInt32(reader.GetOrdinal("applications_applied_target")),
            });
        }

        return rows;
    }

    public async Task<List<CockpitPipelineStep>> GetTodayPipelineStepsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT step_order,
                   step_key,
                   label_zh,
                   label_en,
                   actual_count,
                   target_count,
                   step_status,
                   source_note
            FROM core.cockpit_today_pipeline_steps
            ORDER BY step_order
            """;

        var rows = new List<CockpitPipelineStep>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new CockpitPipelineStep
            {
                StepOrder = reader.GetInt32(reader.GetOrdinal("step_order")),
                StepKey = reader.GetString(reader.GetOrdinal("step_key")),
                LabelZh = reader.GetString(reader.GetOrdinal("label_zh")),
                LabelEn = reader.GetString(reader.GetOrdinal("label_en")),
                ActualCount = reader.GetInt32(reader.GetOrdinal("actual_count")),
                TargetCount = reader.GetInt32(reader.GetOrdinal("target_count")),
                StepStatus = reader.GetString(reader.GetOrdinal("step_status")),
                SourceNote = reader.GetString(reader.GetOrdinal("source_note")),
            });
        }

        return rows;
    }

    public async Task<List<CockpitRecentJob>> GetRecentJobsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT job_id,
                   company,
                   title,
                   location,
                   status_code,
                   disposition_state,
                   match_level,
                   match_score,
                   win_score,
                   judgement_summary,
                   created_at,
                   pipeline_folder
            FROM core.cockpit_recent_jd
            ORDER BY created_at DESC
            """;

        var rows = new List<CockpitRecentJob>();
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new CockpitRecentJob
            {
                JobId = reader.GetString(reader.GetOrdinal("job_id")),
                Company = reader.GetString(reader.GetOrdinal("company")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Location = GetNullableString(reader, "location"),
                StatusCode = reader.GetString(reader.GetOrdinal("status_code")),
                DispositionState = reader.GetString(reader.GetOrdinal("disposition_state")),
                MatchLevel = GetNullableString(reader, "match_level"),
                MatchScore = GetNullableInt32(reader, "match_score"),
                WinScore = GetNullableInt32(reader, "win_score"),
                JudgementSummary = GetNullableString(reader, "judgement_summary") ?? string.Empty,
                CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
                PipelineFolder = GetNullableString(reader, "pipeline_folder"),
            });
        }

        return rows;
    }

    public async Task<CockpitJobProcess?> GetJobProcessAsync(string jobId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT job_id,
                   company,
                   title,
                   status_code,
                   status_detail,
                   disposition_state,
                   disposition_note,
                   match_level,
                   win_reasoning,
                   win_score,
                   ai_reason,
                   ai_summary,
                   visa_note,
                   jd_full_text,
                   pipeline_folder,
                   pjd_role_summary,
                   pjd_hiring_focus
            FROM core.cockpit_jd_process
            WHERE job_id = @jobId
            LIMIT 1
            """;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("jobId", jobId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new CockpitJobProcess
        {
            JobId = reader.GetString(reader.GetOrdinal("job_id")),
            Company = reader.GetString(reader.GetOrdinal("company")),
            Title = reader.GetString(reader.GetOrdinal("title")),
            StatusCode = reader.GetString(reader.GetOrdinal("status_code")),
            StatusDetail = GetNullableString(reader, "status_detail"),
            DispositionState = reader.GetString(reader.GetOrdinal("disposition_state")),
            DispositionNote = GetNullableString(reader, "disposition_note"),
            MatchLevel = GetNullableString(reader, "match_level"),
            WinReasoning = GetNullableString(reader, "win_reasoning"),
            WinScore = GetNullableInt32(reader, "win_score"),
            AiReason = GetNullableString(reader, "ai_reason"),
            AiSummary = GetNullableString(reader, "ai_summary"),
            VisaNote = GetNullableString(reader, "visa_note"),
            JdFullText = GetNullableString(reader, "jd_full_text"),
            PipelineFolder = GetNullableString(reader, "pipeline_folder"),
            PjdRoleSummary = GetNullableString(reader, "pjd_role_summary"),
            PjdHiringFocus = GetNullableString(reader, "pjd_hiring_focus"),
        };
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static string? GetNullableString(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetNullableInt32(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }
}

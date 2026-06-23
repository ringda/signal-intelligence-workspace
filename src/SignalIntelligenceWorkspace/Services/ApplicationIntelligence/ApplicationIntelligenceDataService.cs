using Npgsql;
using SignalIntelligenceWorkspace.Models.ApplicationIntelligence;

namespace SignalIntelligenceWorkspace.Services.ApplicationIntelligence;

public sealed class ApplicationIntelligenceDataService(IConfiguration configuration)
{
    private readonly string _connectionString = configuration["Cockpit:ConnectionString"]
        ?? throw new InvalidOperationException("Cockpit:ConnectionString is not configured.");

    public async Task<ApplicationIntelligenceSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        return new ApplicationIntelligenceSnapshot
        {
            Summary = await GetSummaryAsync(connection, cancellationToken),
            FitSegments = await GetFitSegmentsAsync(connection, cancellationToken),
            CaseRecords = await GetCaseRecordsAsync(connection, cancellationToken),
            LoadedAt = DateTimeOffset.Now,
        };
    }

    private static async Task<ApplicationIntelligenceSummary> GetSummaryAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH settings AS (
              SELECT 'America/Los_Angeles'::text AS tz
            ),
            bounds AS (
              SELECT
                tz,
                (now() AT TIME ZONE tz)::date AS local_today
              FROM settings
            ),
            job_metrics AS (
              SELECT
                count(*) AS jobs_screened_total,
                count(*) FILTER (WHERE j.created_at >= now() - interval '30 days') AS jobs_screened_30d,
                count(*) FILTER (
                  WHERE EXISTS (
                    SELECT 1
                    FROM core.descriptions d
                    WHERE d.job_id = j.job_id
                  )
                ) AS jobs_with_descriptions,
                count(*) FILTER (
                  WHERE j.disposition_state = 'open'
                    OR j.status_code IN ('recommended', 'recommended_edge', 'visa_uncertain', 'applied')
                    OR lower(coalesce(j.match_level, '')) IN ('high', 'strong', 'medium', 'moderate', 'edge')
                ) AS qualified_roles
              FROM core.jobs j
            ),
            application_metrics AS (
              SELECT
                count(*) AS application_rows,
                count(*) FILTER (WHERE a.pa_status = 'applied') AS applications_submitted,
                count(*) FILTER (WHERE a.pjd_status = 'done') AS role_briefed,
                count(*) FILTER (WHERE a.pt_status = 'done' OR a.prp_status = 'done') AS tailored,
                count(*) FILTER (
                  WHERE a.networking_stage IS NOT NULL
                    AND a.networking_stage <> 'not_started'
                ) AS networking_started,
                count(*) FILTER (
                  WHERE a.pa_status = 'applied'
                    AND (a.pa_applied_at IS NOT NULL OR a.applied_date IS NOT NULL)
                ) AS tracker_written,
                count(*) FILTER (
                  WHERE a.pa_status = 'applied'
                    AND a.updated_at >= now() - interval '30 days'
                ) AS learning_signals
              FROM core.applications a
            )
            SELECT jobs_screened_total,
                   jobs_screened_30d,
                   jobs_with_descriptions,
                   qualified_roles,
                   application_rows,
                   applications_submitted,
                   role_briefed,
                   tailored,
                   networking_started,
                   tracker_written,
                   learning_signals,
                   local_today,
                   metric_timezone
            FROM (
              SELECT
                jm.jobs_screened_total,
                jm.jobs_screened_30d,
                jm.jobs_with_descriptions,
                jm.qualified_roles,
                am.application_rows,
                am.applications_submitted,
                am.role_briefed,
                am.tailored,
                am.networking_started,
                am.tracker_written,
                am.learning_signals,
                b.local_today,
                b.tz AS metric_timezone
              FROM job_metrics jm
              CROSS JOIN application_metrics am
              CROSS JOIN bounds b
            ) live_application_intelligence_summary
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new ApplicationIntelligenceSummary();
        }

        return new ApplicationIntelligenceSummary
        {
            JobsScreenedTotal = reader.GetInt64(reader.GetOrdinal("jobs_screened_total")),
            JobsScreened30Days = reader.GetInt64(reader.GetOrdinal("jobs_screened_30d")),
            JobsWithDescriptions = reader.GetInt64(reader.GetOrdinal("jobs_with_descriptions")),
            QualifiedRoles = reader.GetInt64(reader.GetOrdinal("qualified_roles")),
            ApplicationRows = reader.GetInt64(reader.GetOrdinal("application_rows")),
            ApplicationsSubmitted = reader.GetInt64(reader.GetOrdinal("applications_submitted")),
            RoleBriefed = reader.GetInt64(reader.GetOrdinal("role_briefed")),
            Tailored = reader.GetInt64(reader.GetOrdinal("tailored")),
            NetworkingStarted = reader.GetInt64(reader.GetOrdinal("networking_started")),
            TrackerWritten = reader.GetInt64(reader.GetOrdinal("tracker_written")),
            LearningSignals = reader.GetInt64(reader.GetOrdinal("learning_signals")),
            LocalToday = reader.GetFieldValue<DateOnly>(reader.GetOrdinal("local_today")),
            MetricTimezone = reader.GetString(reader.GetOrdinal("metric_timezone")),
        };
    }

    private static async Task<List<ApplicationIntelligenceFitSegment>> GetFitSegmentsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH normalized_jobs AS (
              SELECT
                CASE
                  WHEN j.status_code = 'applied' THEN 'applied'
                  WHEN j.status_code = 'recommended'
                    OR j.disposition_state = 'open'
                    OR lower(coalesce(j.match_level, '')) IN ('high', 'strong') THEN 'strong'
                  WHEN j.status_code = 'recommended_edge'
                    OR lower(coalesce(j.match_level, '')) IN ('medium', 'moderate', 'edge') THEN 'possible'
                  WHEN j.status_code IN ('on_hold', 'pending_analysis', 'pending_visa', 'visa_uncertain') THEN 'review'
                  ELSE 'other'
                END AS segment_key,
                CASE
                  WHEN coalesce(j.win_score, j.match_score, 0) BETWEEN 1 AND 10
                    THEN coalesce(j.win_score, j.match_score, 0) * 10
                  ELSE greatest(0, least(100, coalesce(j.win_score, j.match_score, 0)))
                END AS readiness_score
              FROM core.jobs j
            )
            SELECT segment_key,
                   count(*)::int AS segment_count,
                   coalesce(round(avg(readiness_score)), 0)::int AS average_readiness
            FROM normalized_jobs
            GROUP BY segment_key
            ORDER BY CASE segment_key
              WHEN 'strong' THEN 1
              WHEN 'possible' THEN 2
              WHEN 'review' THEN 3
              WHEN 'applied' THEN 4
              ELSE 5
            END
            """;

        var rows = new List<ApplicationIntelligenceFitSegment>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ApplicationIntelligenceFitSegment
            {
                SegmentKey = reader.GetString(reader.GetOrdinal("segment_key")),
                Count = reader.GetInt32(reader.GetOrdinal("segment_count")),
                AverageReadiness = reader.GetInt32(reader.GetOrdinal("average_readiness")),
            });
        }

        return rows;
    }

    private static async Task<List<ApplicationIntelligenceCaseRecord>> GetCaseRecordsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            WITH settings AS (
              SELECT 'America/Los_Angeles'::text AS tz
            ),
            safe_cases AS (
              SELECT
                j.job_id,
                coalesce(nullif(j.company, ''), 'Unknown company') AS company,
                coalesce(nullif(j.title, ''), 'Unknown role') AS title,
                coalesce(j.status_code, '') AS status_code,
                coalesce(j.disposition_state, '') AS disposition_state,
                nullif(j.match_level, '') AS match_level,
                j.match_score,
                j.win_score,
                left(coalesce(j.win_reasoning, j.ai_reason, j.ai_summary, j.disposition_note, ''), 260) AS judgement_summary,
                left(coalesce(d.description, ''), 320) AS jd_excerpt,
                nullif(a.pipeline_folder, '') AS pipeline_folder,
                coalesce(a.pjd_status, 'not_started') AS pjd_status,
                coalesce(a.pt_status, 'not_started') AS pt_status,
                coalesce(a.prp_status, 'not_started') AS prp_status,
                coalesce(a.pa_status, 'not_started') AS pa_status,
                coalesce(a.networking_stage, 'not_started') AS networking_stage,
                left(coalesce(a.pjd_role_summary, ''), 260) AS pjd_role_summary,
                left(coalesce(a.pjd_hiring_focus, ''), 260) AS pjd_hiring_focus,
                coalesce(a.pa_applied_at, a.applied_date::timestamp AT TIME ZONE s.tz) AS applied_at,
                a.updated_at,
                CASE
                  WHEN a.pa_status = 'applied' THEN 50 ELSE 0
                END
                + CASE
                  WHEN a.pjd_status = 'done' THEN 20 ELSE 0
                END
                + CASE
                  WHEN a.pt_status = 'done' OR a.prp_status = 'done' THEN 20 ELSE 0
                END
                + CASE
                  WHEN a.networking_stage IS NOT NULL AND a.networking_stage <> 'not_started' THEN 10 ELSE 0
                END AS workflow_rank
              FROM core.applications a
              JOIN core.jobs j ON j.job_id = a.job_id
              CROSS JOIN settings s
              LEFT JOIN LATERAL (
                SELECT d.description
                FROM core.descriptions d
                WHERE d.job_id = j.job_id
                LIMIT 1
              ) d ON TRUE
            )
            SELECT job_id,
                   company,
                   title,
                   status_code,
                   disposition_state,
                   match_level,
                   match_score,
                   win_score,
                   judgement_summary,
                   jd_excerpt,
                   pipeline_folder,
                   pjd_status,
                   pt_status,
                   prp_status,
                   pa_status,
                   networking_stage,
                   pjd_role_summary,
                   pjd_hiring_focus,
                   applied_at,
                   updated_at
            FROM safe_cases
            WHERE company <> 'Unknown company'
              AND title <> 'Unknown role'
            ORDER BY workflow_rank DESC,
                     updated_at DESC NULLS LAST
            LIMIT 8
            """;

        var rows = new List<ApplicationIntelligenceCaseRecord>();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new ApplicationIntelligenceCaseRecord
            {
                JobId = reader.GetString(reader.GetOrdinal("job_id")),
                Company = reader.GetString(reader.GetOrdinal("company")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                StatusCode = reader.GetString(reader.GetOrdinal("status_code")),
                DispositionState = reader.GetString(reader.GetOrdinal("disposition_state")),
                MatchLevel = GetNullableString(reader, "match_level"),
                MatchScore = GetNullableInt32(reader, "match_score"),
                WinScore = GetNullableInt32(reader, "win_score"),
                JudgementSummary = GetNullableString(reader, "judgement_summary"),
                JdExcerpt = GetNullableString(reader, "jd_excerpt"),
                PipelineFolder = GetNullableString(reader, "pipeline_folder"),
                PjdStatus = reader.GetString(reader.GetOrdinal("pjd_status")),
                PtStatus = reader.GetString(reader.GetOrdinal("pt_status")),
                PrpStatus = reader.GetString(reader.GetOrdinal("prp_status")),
                PaStatus = reader.GetString(reader.GetOrdinal("pa_status")),
                NetworkingStage = reader.GetString(reader.GetOrdinal("networking_stage")),
                PjdRoleSummary = GetNullableString(reader, "pjd_role_summary"),
                PjdHiringFocus = GetNullableString(reader, "pjd_hiring_focus"),
                AppliedAt = GetNullableDateTimeOffset(reader, "applied_at"),
                UpdatedAt = GetNullableDateTimeOffset(reader, "updated_at"),
            });
        }

        return rows;
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

    private static DateTimeOffset? GetNullableDateTimeOffset(NpgsqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetFieldValue<DateTimeOffset>(ordinal);
    }
}

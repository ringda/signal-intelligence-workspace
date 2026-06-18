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
            WITH settings AS (
              SELECT 'America/Los_Angeles'::text AS tz
            ),
            bounds AS (
              SELECT
                tz,
                (date_trunc('day', now() AT TIME ZONE tz) AT TIME ZONE tz) AS today_start,
                (date_trunc('week', now() AT TIME ZONE tz) AT TIME ZONE tz) AS week_start,
                (now() AT TIME ZONE tz)::date AS local_today,
                date_trunc('week', now() AT TIME ZONE tz)::date AS local_week_start
              FROM settings
            ),
            job_metrics AS (
              SELECT
                count(*) FILTER (WHERE j.created_at >= now() - interval '24 hours') AS jd_scanned_24h,
                3 AS jd_scanned_target,
                count(*) FILTER (
                  WHERE j.created_at >= now() - interval '24 hours'
                    AND j.disposition_state = 'open'
                ) AS jd_correct_pool_24h,
                count(*) FILTER (WHERE j.created_at >= b.week_start) AS jd_scanned_week,
                count(*) FILTER (WHERE j.created_at >= b.today_start) AS jd_scanned_today,
                count(*) FILTER (
                  WHERE j.created_at >= b.today_start
                    AND j.disposition_state = 'open'
                ) AS jd_correct_pool_today
              FROM core.jobs j
              CROSS JOIN bounds b
            ),
            application_events AS (
              SELECT
                a.*,
                coalesce(a.pa_applied_at, a.applied_date::timestamp AT TIME ZONE b.tz) AS applied_at
              FROM core.applications a
              CROSS JOIN bounds b
            ),
            application_metrics AS (
              SELECT
                count(*) FILTER (
                  WHERE a.pa_status = 'applied'
                    AND a.applied_at >= b.today_start
                ) AS applications_applied_today,
                3 AS applications_applied_target,
                count(*) FILTER (
                  WHERE a.pa_status = 'applied'
                    AND a.applied_at >= b.week_start
                ) AS applications_applied_week,
                15 AS applications_applied_week_target,
                count(*) FILTER (
                  WHERE a.pjd_status = 'done'
                    AND a.pjd_at >= b.today_start
                ) AS applications_role_briefed_today,
                count(*) FILTER (
                  WHERE (
                      (a.pt_status = 'done' AND a.pt_at >= b.today_start)
                      OR (a.prp_status = 'done' AND a.prp_at >= b.today_start)
                    )
                ) AS applications_resume_customized_today,
                count(*) FILTER (
                  WHERE a.networking_stage IS NOT NULL
                    AND a.networking_stage <> 'not_started'
                    AND a.updated_at >= b.today_start
                ) AS applications_networking_started_today
              FROM application_events a
              CROSS JOIN bounds b
            )
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
            FROM (
              SELECT
                jm.jd_scanned_24h,
                jm.jd_scanned_target,
                jm.jd_correct_pool_24h,
                jm.jd_scanned_week,
                am.applications_applied_today,
                am.applications_applied_target,
                am.applications_applied_week,
                am.applications_applied_week_target,
                jm.jd_scanned_today,
                jm.jd_correct_pool_today,
                am.applications_role_briefed_today,
                am.applications_resume_customized_today,
                am.applications_networking_started_today,
                b.local_today,
                b.local_week_start,
                b.tz AS metric_timezone
              FROM job_metrics jm
              CROSS JOIN application_metrics am
              CROSS JOIN bounds b
            ) direct_metrics
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
            WITH settings AS (
              SELECT 'America/Los_Angeles'::text AS tz
            ),
            days AS (
              SELECT
                gs::date AS day_local
              FROM settings s
              CROSS JOIN LATERAL generate_series(
                date_trunc('week', now() AT TIME ZONE s.tz)::date,
                date_trunc('week', now() AT TIME ZONE s.tz)::date + 6,
                interval '1 day'
              ) AS gs
            ),
            job_counts AS (
              SELECT
                (j.created_at AT TIME ZONE s.tz)::date AS day_local,
                count(*)::int AS jd_scanned,
                count(*) FILTER (WHERE j.disposition_state = 'open')::int AS jd_correct_pool
              FROM core.jobs j
              CROSS JOIN settings s
              WHERE (j.created_at AT TIME ZONE s.tz)::date >= date_trunc('week', now() AT TIME ZONE s.tz)::date
                AND (j.created_at AT TIME ZONE s.tz)::date <= date_trunc('week', now() AT TIME ZONE s.tz)::date + 6
              GROUP BY (j.created_at AT TIME ZONE s.tz)::date
            ),
            application_counts AS (
              SELECT
                (coalesce(a.pa_applied_at, a.applied_date::timestamp AT TIME ZONE s.tz) AT TIME ZONE s.tz)::date AS day_local,
                count(*) FILTER (WHERE a.pa_status = 'applied')::int AS applications_applied
              FROM core.applications a
              CROSS JOIN settings s
              WHERE coalesce(a.pa_applied_at, a.applied_date::timestamp AT TIME ZONE s.tz) IS NOT NULL
                AND (coalesce(a.pa_applied_at, a.applied_date::timestamp AT TIME ZONE s.tz) AT TIME ZONE s.tz)::date
                    >= date_trunc('week', now() AT TIME ZONE s.tz)::date
                AND (coalesce(a.pa_applied_at, a.applied_date::timestamp AT TIME ZONE s.tz) AT TIME ZONE s.tz)::date
                    <= date_trunc('week', now() AT TIME ZONE s.tz)::date + 6
              GROUP BY (coalesce(a.pa_applied_at, a.applied_date::timestamp AT TIME ZONE s.tz) AT TIME ZONE s.tz)::date
            )
            SELECT day,
                   jd_scanned,
                   jd_scanned_target,
                   jd_correct_pool,
                   applications_applied,
                   applications_applied_target
            FROM (
              SELECT
                d.day_local::timestamp AS day,
                coalesce(j.jd_scanned, 0) AS jd_scanned,
                3 AS jd_scanned_target,
                coalesce(j.jd_correct_pool, 0) AS jd_correct_pool,
                coalesce(a.applications_applied, 0) AS applications_applied,
                3 AS applications_applied_target
              FROM days d
              LEFT JOIN job_counts j ON j.day_local = d.day_local
              LEFT JOIN application_counts a ON a.day_local = d.day_local
            ) direct_series
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
            WITH settings AS (
              SELECT 'America/Los_Angeles'::text AS tz
            ),
            bounds AS (
              SELECT
                tz,
                (date_trunc('day', now() AT TIME ZONE tz) AT TIME ZONE tz) AS today_start
              FROM settings
            ),
            application_events AS (
              SELECT
                a.*,
                coalesce(a.pa_applied_at, a.applied_date::timestamp AT TIME ZONE b.tz) AS applied_at
              FROM core.applications a
              CROSS JOIN bounds b
            ),
            counts AS (
              SELECT
                (
                  SELECT count(*)::int
                  FROM core.jobs j
                  CROSS JOIN bounds b
                  WHERE j.created_at >= b.today_start
                    AND j.disposition_state = 'open'
                ) AS role_selected,
                (
                  SELECT count(*)::int
                  FROM application_events a
                  CROSS JOIN bounds b
                  WHERE a.pjd_status = 'done'
                    AND a.pjd_at >= b.today_start
                ) AS role_briefed,
                (
                  SELECT count(*)::int
                  FROM application_events a
                  CROSS JOIN bounds b
                  WHERE (a.pt_status = 'done' AND a.pt_at >= b.today_start)
                     OR (a.prp_status = 'done' AND a.prp_at >= b.today_start)
                ) AS resume_customized,
                (
                  SELECT count(*)::int
                  FROM application_events a
                  CROSS JOIN bounds b
                  WHERE a.pa_status = 'applied'
                    AND a.applied_at >= b.today_start
                ) AS applied,
                (
                  SELECT count(*)::int
                  FROM application_events a
                  CROSS JOIN bounds b
                  WHERE a.networking_stage IS NOT NULL
                    AND a.networking_stage <> 'not_started'
                    AND a.updated_at >= b.today_start
                ) AS networking_started,
                (
                  SELECT count(*)::int
                  FROM application_events a
                  CROSS JOIN bounds b
                  WHERE a.pa_status = 'applied'
                    AND a.applied_at >= b.today_start
                    AND (a.pa_applied_at IS NOT NULL OR a.applied_date IS NOT NULL)
                ) AS tracker_written
            ),
            steps AS (
              SELECT 1 AS step_order, 'role_selected' AS step_key, '選對 role' AS label_zh, 'Role selected' AS label_en,
                     role_selected AS actual_count, 3 AS target_count,
                     'From core.jobs disposition_state=open; today in America/Los_Angeles.' AS source_note
              FROM counts
              UNION ALL
              SELECT 2, 'custom_cv', '客製 CV', 'Custom CV',
                     resume_customized, greatest(applied, 1),
                     'From pt_status/prp_status timestamps; target follows today applied count, minimum 1.'
              FROM counts
              UNION ALL
              SELECT 3, 'formal_apply', '正式投遞', 'Formal apply',
                     applied, 3,
                     'From core.applications pa_status=applied and pa_applied_at/applied_date.'
              FROM counts
              UNION ALL
              SELECT 4, 'networking_started', 'networking 啟動', 'Networking started',
                     networking_started, applied,
                     'No canonical contact count yet; downgraded from >=5 contacts to networking_stage started.'
              FROM counts
              UNION ALL
              SELECT 5, 'tracker_written', '回寫 tracker', 'Tracker written',
                     tracker_written, applied,
                     'From pa_status=applied plus pa_applied_at/applied_date present.'
              FROM counts
            )
            SELECT step_order,
                   step_key,
                   label_zh,
                   label_en,
                   actual_count,
                   target_count,
                   step_status,
                   source_note
            FROM (
              SELECT
                step_order,
                step_key,
                label_zh,
                label_en,
                actual_count,
                target_count,
                CASE
                  WHEN target_count = 0 THEN 'not_started'
                  WHEN actual_count >= target_count THEN 'complete'
                  WHEN actual_count > 0 THEN 'in_progress'
                  ELSE 'not_started'
                END AS step_status,
                source_note
              FROM steps
            ) direct_steps
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
            FROM (
              SELECT
                j.job_id,
                j.company,
                j.title,
                j.location,
                j.status_code,
                j.disposition_state,
                j.match_level,
                j.match_score,
                j.win_score,
                left(coalesce(j.win_reasoning, j.ai_reason, j.ai_summary, ''), 140) AS judgement_summary,
                j.created_at,
                a.pipeline_folder
              FROM core.jobs j
              LEFT JOIN core.applications a ON a.job_id = j.job_id
              WHERE j.created_at >= now() - interval '7 days'
            ) direct_recent_jd
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
            FROM (
              SELECT
                j.job_id,
                j.company,
                j.title,
                j.status_code,
                j.status_detail,
                j.disposition_state,
                j.disposition_note,
                j.match_level,
                j.win_reasoning,
                j.win_score,
                j.ai_reason,
                j.ai_summary,
                j.visa_note,
                d.description AS jd_full_text,
                a.pipeline_folder,
                a.pjd_role_summary,
                a.pjd_hiring_focus
              FROM core.jobs j
              LEFT JOIN core.descriptions d ON d.job_id = j.job_id
              LEFT JOIN core.applications a ON a.job_id = j.job_id
            ) direct_jd_process
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

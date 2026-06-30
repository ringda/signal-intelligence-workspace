namespace SignalIntelligenceWorkspace.Services.PublicCadence;

public sealed record PublicOperatingCadenceSnapshot(
    IReadOnlyList<PublicOperatingCadenceDay> Days,
    IReadOnlyList<PublicOperatingCadenceRow> Rows,
    DateTimeOffset GeneratedAt,
    string DisplayTimezone)
{
    public static PublicOperatingCadenceSnapshot Fallback(DateTimeOffset generatedAt)
    {
        var days = new[]
        {
            new PublicOperatingCadenceDay(1, string.Empty, "Mon"),
            new PublicOperatingCadenceDay(2, string.Empty, "Tue"),
            new PublicOperatingCadenceDay(3, string.Empty, "Wed"),
            new PublicOperatingCadenceDay(4, string.Empty, "Thu"),
            new PublicOperatingCadenceDay(5, string.Empty, "Fri"),
        };

        var rows = PublicOperatingCadenceDefinitions.DbBackedSteps
            .Select(step => new PublicOperatingCadenceRow(
                step.StepKey,
                step.Label,
                true,
                days.Select(day => new PublicOperatingCadenceCell(
                    day.DayPosition,
                    day.DisplayDayKey,
                    day.WeekdayLabel,
                    "none")).ToArray()))
            .ToArray();

        return new PublicOperatingCadenceSnapshot(days, rows, generatedAt, "static-fallback");
    }
}

public sealed record PublicOperatingCadenceDay(
    int DayPosition,
    string DisplayDayKey,
    string WeekdayLabel);

public sealed record PublicOperatingCadenceRow(
    string StepKey,
    string Label,
    bool IsDbBacked,
    IReadOnlyList<PublicOperatingCadenceCell> Cells);

public sealed record PublicOperatingCadenceCell(
    int DayPosition,
    string DisplayDayKey,
    string WeekdayLabel,
    string ActivityLevel);

public sealed record PublicOperatingCadenceStep(
    int Order,
    string StepKey,
    string Label);

public static class PublicOperatingCadenceDefinitions
{
    public static readonly IReadOnlyList<PublicOperatingCadenceStep> Steps =
    [
        new(1, "signal_scan", "Signal Scan"),
        new(2, "role_context", "Role Context"),
        new(3, "proof_build", "Proof Build"),
        new(4, "release", "Release"),
        new(5, "human_feedback", "Human Feedback"),
        new(6, "write_back", "Write-back"),
    ];

    public static readonly IReadOnlyList<PublicOperatingCadenceStep> DbBackedSteps =
        Steps.Where(step => step.StepKey != "write_back").ToArray();
}

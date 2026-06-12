using System.Text;
using SignalIntelligenceWorkspace.Models;

namespace SignalIntelligenceWorkspace.Services;

/// <summary>
/// Deterministic draft generation — derived entirely from scenario data.
/// No randomness, no clock: the same command against the same data yields the same draft.
/// </summary>
public static class DraftComposer
{
    public static (string Title, string Body) Compose(SafeCommand command, ScenarioPack scenario) =>
        command switch
        {
            SummarizeFeedbackCommand c => SummarizeFeedback(c, scenario),
            DraftInsightMemoCommand c => InsightMemo(c, scenario),
            CompareMarketSegmentsCommand c => CompareSegments(c, scenario),
            _ => ("Draft", "No draft template exists for this command."),
        };

    private static (string, string) SummarizeFeedback(SummarizeFeedbackCommand command, ScenarioPack scenario)
    {
        var top = scenario.Themes.OrderByDescending(t => t.Frequency).Take(3).ToList();
        var sb = new StringBuilder();
        sb.AppendLine($"Recurring feedback themes for the {command.Audience}:");
        foreach (var theme in top)
        {
            sb.AppendLine();
            sb.AppendLine($"- {theme.Theme} ({theme.Segment}, mentioned {theme.Frequency} times)");
            sb.AppendLine($"  Example: \"{theme.Example}\"");
            sb.AppendLine($"  Suggested use: {theme.SuggestedUse}");
        }

        return ("Feedback summary for the proposal team", sb.ToString().TrimEnd());
    }

    private static (string, string) InsightMemo(DraftInsightMemoCommand command, ScenarioPack scenario)
    {
        var themes = scenario.Themes
            .Where(t => t.Segment.Equals(command.Segment, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Frequency)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"Insight memo — {command.Segment} proposal planning");
        sb.AppendLine();
        if (themes.Count == 0)
        {
            sb.AppendLine("No recorded signals for this segment yet. Recommend collecting more inputs before drafting positioning.");
        }
        else
        {
            sb.AppendLine($"Signals on record: {themes.Count}; total mentions: {themes.Sum(t => t.Frequency)}.");
            foreach (var theme in themes)
            {
                sb.AppendLine($"- {theme.Theme} (confidence: {theme.Confidence}) — {theme.SuggestedUse}");
            }
        }

        return ($"Insight memo: {command.Segment}", sb.ToString().TrimEnd());
    }

    private static (string, string) CompareSegments(CompareMarketSegmentsCommand command, ScenarioPack scenario)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Market segment comparison:");
        foreach (var segment in command.Segments)
        {
            var themes = scenario.Themes
                .Where(t => t.Segment.Equals(segment, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var dominant = themes
                .GroupBy(t => t.Confidence)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key.ToString())
                .FirstOrDefault() ?? "n/a";
            sb.AppendLine($"- {segment}: {themes.Count} themes, {themes.Sum(t => t.Frequency)} mentions, dominant confidence {dominant}");
        }

        return ("Segment comparison", sb.ToString().TrimEnd());
    }
}

using System.Text.RegularExpressions;
using SignalIntelligenceWorkspace.Models;

namespace SignalIntelligenceWorkspace.Services;

/// <summary>
/// Deterministic prompt-to-command parser. Pure and stateless: same prompt, same result.
/// Evaluation order is deny-before-allow — forbidden rules run first so a prompt that
/// mixes a legitimate request with a forbidden action is rejected whole.
/// </summary>
public static class CommandParser
{
    private sealed record ForbiddenRule(Regex Pattern, string Name, string Reason);

    private static readonly ForbiddenRule[] ForbiddenRules =
    [
        new(new Regex(@"\b(delete|remove|drop|purge|erase)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "deleteData", "Destructive operations are outside the safe command whitelist."),
        new(new Regex(@"\b(send|email|forward)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "sendExternally", "Outbound delivery is not permitted from this workspace."),
        new(new Regex(@"\b(write back|writeback|update the source|push to|sync to)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "writeToSourceSystem", "Source systems are read-only for AI commands."),
        new(new Regex(@"\b(sql|query the database)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "rawSql", "Raw queries bypass the command schema."),
        new(new Regex(@"\b(api key|credential|secret|token|password)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "getApiKey", "Credential access is never a valid command."),
        new(new Regex(@"\b(execute code|run (a |the )?script|eval|shell)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "executeCode", "Arbitrary execution is outside the whitelist."),
    ];

    public static ParseResult Parse(string prompt, IReadOnlyList<string> segments)
    {
        var raw = prompt?.Trim() ?? string.Empty;
        if (raw.Length == 0)
        {
            return ParseResult.Unmatched("Empty prompt. Try one of the demo prompts.", raw);
        }

        foreach (var rule in ForbiddenRules)
        {
            if (rule.Pattern.IsMatch(raw))
            {
                return ParseResult.Forbidden(rule.Name, rule.Reason, raw);
            }
        }

        // Allow rules — first match wins.
        if (Matches(raw, @"summari[sz]e") && Matches(raw, @"feedback"))
        {
            return ParseResult.Allowed(new SummarizeFeedbackCommand("proposal team"), raw);
        }

        var confidence = MatchConfidence(raw);
        if (confidence is not null && Matches(raw, @"(signal|opportunit|show|filter)"))
        {
            return ParseResult.Allowed(new FilterGridCommand("themes", "confidence", confidence), raw);
        }

        if (Matches(raw, @"\bdraft\b") && Matches(raw, @"\bmemo\b"))
        {
            var segment = segments.FirstOrDefault(s => raw.Contains(s, StringComparison.OrdinalIgnoreCase))
                          ?? segments[0];
            return ParseResult.Allowed(new DraftInsightMemoCommand(segment), raw);
        }

        if (Matches(raw, @"\bcompare\b") && Matches(raw, @"(segment|market)"))
        {
            var mentioned = segments.Where(s => raw.Contains(s, StringComparison.OrdinalIgnoreCase)).ToList();
            return ParseResult.Allowed(
                new CompareMarketSegmentsCommand(mentioned.Count > 0 ? mentioned : segments.ToList()), raw);
        }

        if (Matches(raw, @"needs review|\bflag\b"))
        {
            return ParseResult.Allowed(new MarkNeedsReviewCommand("latest-draft"), raw);
        }

        if (Matches(raw, @"\bapprove\b"))
        {
            return ParseResult.Allowed(new ApproveDraftCommand("latest-draft"), raw);
        }

        if (Matches(raw, @"\breject\b"))
        {
            return ParseResult.Allowed(new RejectDraftCommand("latest-draft"), raw);
        }

        return ParseResult.Unmatched(
            "No safe command matched this prompt. Try one of the demo prompts or rephrase.", raw);
    }

    private static bool Matches(string input, string pattern) =>
        Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase);

    private static string? MatchConfidence(string raw)
    {
        if (Matches(raw, @"high[-\s]?confidence")) return "High";
        if (Matches(raw, @"medium[-\s]?confidence")) return "Medium";
        if (Matches(raw, @"low[-\s]?confidence")) return "Low";
        return null;
    }
}

namespace SignalIntelligenceWorkspace.Models;

public enum ParseOutcome
{
    Allowed,
    Forbidden,
    Unmatched,
}

public sealed record ParseResult(
    ParseOutcome Outcome,
    SafeCommand? Command,
    string? BlockedRuleName,
    string? Reason,
    string RawPrompt)
{
    public static ParseResult Allowed(SafeCommand command, string rawPrompt) =>
        new(ParseOutcome.Allowed, command, null, null, rawPrompt);

    public static ParseResult Forbidden(string ruleName, string reason, string rawPrompt) =>
        new(ParseOutcome.Forbidden, null, ruleName, reason, rawPrompt);

    public static ParseResult Unmatched(string message, string rawPrompt) =>
        new(ParseOutcome.Unmatched, null, null, message, rawPrompt);
}

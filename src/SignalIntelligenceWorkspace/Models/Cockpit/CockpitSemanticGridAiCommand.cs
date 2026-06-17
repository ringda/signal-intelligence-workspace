using System.Text.Json.Serialization;

namespace SignalIntelligenceWorkspace.Models.Cockpit;

public sealed record CockpitSemanticGridAiCommand
{
    [JsonPropertyName("matched")]
    public bool Matched { get; init; }

    [JsonPropertyName("clear")]
    public bool Clear { get; init; }

    [JsonPropertyName("filter")]
    public string? Filter { get; init; }

    [JsonPropertyName("dateWindow")]
    public string? DateWindow { get; init; }

    [JsonPropertyName("sort")]
    public string? Sort { get; init; }

    [JsonPropertyName("contentQuery")]
    public string? ContentQuery { get; init; }

    [JsonPropertyName("contentTerms")]
    public IReadOnlyList<string> ContentTerms { get; init; } = [];

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}

public sealed record CockpitSemanticGridAiParseResult(
    bool Attempted,
    CockpitSemanticGridAiCommand? Command,
    string? Error)
{
    public static CockpitSemanticGridAiParseResult NotConfigured() =>
        new(false, null, null);

    public static CockpitSemanticGridAiParseResult Parsed(CockpitSemanticGridAiCommand command) =>
        new(true, command, null);

    public static CockpitSemanticGridAiParseResult Failed(string error) =>
        new(true, null, error);
}

public sealed record CockpitAssistantAiResult(
    bool Attempted,
    string? Message,
    string? Error)
{
    public static CockpitAssistantAiResult NotConfigured() =>
        new(false, null, null);

    public static CockpitAssistantAiResult Answered(string message) =>
        new(true, message, null);

    public static CockpitAssistantAiResult Failed(string error) =>
        new(true, null, error);
}

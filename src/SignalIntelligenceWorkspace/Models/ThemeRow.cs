namespace SignalIntelligenceWorkspace.Models;

public sealed class ThemeRow
{
    public required string Id { get; init; }
    public required string Theme { get; init; }
    public required string Segment { get; init; }
    public required int Frequency { get; init; }
    public required Confidence Confidence { get; init; }
    public required string Example { get; init; }
    public required string SuggestedUse { get; init; }
    public required ReviewState ReviewStatus { get; init; }
}

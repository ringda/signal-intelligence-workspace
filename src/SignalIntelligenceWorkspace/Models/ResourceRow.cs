namespace SignalIntelligenceWorkspace.Models;

public sealed class ResourceRow
{
    public required string Id { get; init; }
    public required string ResourceType { get; init; }
    public required string Title { get; init; }
    public required string ClientProject { get; init; }
    public required string MarketSegment { get; init; }
    public required string Status { get; init; }
    public required string Owner { get; init; }
    public required DateOnly LastUpdated { get; init; }
    public required ReviewState ReviewState { get; init; }
}

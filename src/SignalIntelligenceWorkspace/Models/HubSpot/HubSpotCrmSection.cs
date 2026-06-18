namespace SignalIntelligenceWorkspace.Models.HubSpot;

public sealed class HubSpotCrmSection
{
    public string Title { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string ObjectTypeId { get; init; } = string.Empty;
    public string IndexUrl { get; init; } = string.Empty;
    public IReadOnlyList<HubSpotCrmRow> Rows { get; init; } = [];
    public string? Error { get; init; }
}

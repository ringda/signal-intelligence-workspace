namespace SignalIntelligenceWorkspace.Models.HubSpot;

public sealed class HubSpotCrmSnapshot
{
    public IReadOnlyList<HubSpotCrmSection> Sections { get; init; } = [];
    public IReadOnlyList<HubSpotReadinessCard> ReadinessCards { get; init; } = [];
    public string PortalUrl { get; init; } = string.Empty;
    public DateTimeOffset RefreshedAt { get; init; }
}

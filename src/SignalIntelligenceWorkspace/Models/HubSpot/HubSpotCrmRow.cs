namespace SignalIntelligenceWorkspace.Models.HubSpot;

public sealed class HubSpotCrmRow
{
    public string Id { get; init; } = string.Empty;
    public string Primary { get; init; } = string.Empty;
    public string Secondary { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public string HubSpotUrl { get; init; } = string.Empty;
}

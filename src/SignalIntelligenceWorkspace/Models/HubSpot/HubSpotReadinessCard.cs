namespace SignalIntelligenceWorkspace.Models.HubSpot;

public sealed class HubSpotReadinessCard
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Lifecycle { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;
    public int Score { get; init; }
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<string> Missing { get; init; } = [];
    public string NextAction { get; init; } = string.Empty;
    public string Why { get; init; } = string.Empty;
    public string SuggestedTaskTitle { get; init; } = string.Empty;
    public string SuggestedNoteBody { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public string HubSpotUrl { get; init; } = string.Empty;
}

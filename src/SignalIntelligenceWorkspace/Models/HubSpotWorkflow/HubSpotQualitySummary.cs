namespace SignalIntelligenceWorkspace.Models.HubSpotWorkflow;

public sealed class HubSpotQualitySummary
{
    public string StatusLabel { get; init; } = string.Empty;
    public string StatusDetail { get; init; } = string.Empty;
    public int RecordsScanned { get; init; }
    public int ActionableProposals { get; init; }
    public int NeedsHumanReview { get; init; }
    public int BlockedProposals { get; init; }
    public int UnsafeWrites { get; init; }
}

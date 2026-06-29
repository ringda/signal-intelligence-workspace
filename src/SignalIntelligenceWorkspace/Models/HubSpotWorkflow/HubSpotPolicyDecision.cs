namespace SignalIntelligenceWorkspace.Models.HubSpotWorkflow;

public sealed class HubSpotPolicyDecision
{
    public HubSpotPolicyStatus Status { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public bool CanExecuteWriteback { get; init; }
}

namespace SignalIntelligenceWorkspace.Models.HubSpotWorkflow;

public sealed class HubSpotWorkflowAuditEvent
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string ProposalId { get; init; } = string.Empty;
    public string Decision { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

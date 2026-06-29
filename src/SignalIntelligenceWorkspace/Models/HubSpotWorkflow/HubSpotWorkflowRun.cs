namespace SignalIntelligenceWorkspace.Models.HubSpotWorkflow;

public sealed class HubSpotWorkflowRun
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset RefreshedAt { get; init; }
    public IReadOnlyDictionary<string, int> SourceCounts { get; init; } = new Dictionary<string, int>();
    public HubSpotQualitySummary Quality { get; init; } = new();
    public IReadOnlyList<HubSpotActionProposal> Proposals { get; init; } = [];
    public IReadOnlyList<HubSpotWorkflowAuditEvent> AuditEvents { get; init; } = [];
}

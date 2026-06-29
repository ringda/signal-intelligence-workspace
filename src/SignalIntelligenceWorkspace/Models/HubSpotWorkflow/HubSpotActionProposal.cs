namespace SignalIntelligenceWorkspace.Models.HubSpotWorkflow;

public sealed class HubSpotActionProposal
{
    public string Id { get; init; } = string.Empty;
    public HubSpotProposalKind Kind { get; init; }
    public HubSpotProposalPriority Priority { get; init; }
    public string ObjectType { get; init; } = string.Empty;
    public string ObjectId { get; init; } = string.Empty;
    public string RecordName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string EvidenceSummary { get; init; } = string.Empty;
    public IReadOnlyList<string> Evidence { get; init; } = [];
    public string ProposedAction { get; init; } = string.Empty;
    public string SuggestedNoteBody { get; init; } = string.Empty;
    public HubSpotPolicyDecision Policy { get; init; } = new();
    public string HubSpotUrl { get; init; } = string.Empty;
}

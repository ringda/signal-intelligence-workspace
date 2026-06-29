using SignalIntelligenceWorkspace.Models.HubSpotWorkflow;

namespace SignalIntelligenceWorkspace.Services.HubSpotWorkflow;

public sealed class HubSpotWorkflowAuditLog
{
    public IReadOnlyList<HubSpotWorkflowAuditEvent> BuildEvents(
        DateTimeOffset timestamp,
        IReadOnlyList<HubSpotActionProposal> proposals)
    {
        var events = new List<HubSpotWorkflowAuditEvent>();
        var nextId = 1;

        foreach (var proposal in proposals)
        {
            events.Insert(0, new HubSpotWorkflowAuditEvent
            {
                Id = nextId.ToString("D4"),
                Timestamp = timestamp.AddMilliseconds(nextId),
                EventType = "ProposalGenerated",
                ProposalId = proposal.Id,
                Decision = proposal.Policy.Label,
                Reason = proposal.Policy.Reason,
            });
            nextId++;
        }

        return events;
    }
}

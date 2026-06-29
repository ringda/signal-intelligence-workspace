using SignalIntelligenceWorkspace.Models.HubSpot;
using SignalIntelligenceWorkspace.Models.HubSpotWorkflow;

namespace SignalIntelligenceWorkspace.Services.HubSpotWorkflow;

public sealed class HubSpotWorkflowEngine(
    HubSpotProposalBuilder proposalBuilder,
    HubSpotWorkflowAuditLog auditLog,
    TimeProvider timeProvider)
{
    public HubSpotWorkflowRun Run(HubSpotCrmSnapshot snapshot)
    {
        var now = timeProvider.GetLocalNow();
        var proposals = proposalBuilder.Build(snapshot);

        return new HubSpotWorkflowRun
        {
            Id = $"hubspot-{now:yyyyMMddHHmmss}",
            RefreshedAt = now,
            SourceCounts = snapshot.Sections.ToDictionary(section => section.Title, section => section.Rows.Count),
            Quality = BuildQuality(snapshot, proposals),
            Proposals = proposals,
            AuditEvents = auditLog.BuildEvents(now, proposals),
        };
    }

    private static HubSpotQualitySummary BuildQuality(
        HubSpotCrmSnapshot snapshot,
        IReadOnlyCollection<HubSpotActionProposal> proposals)
    {
        var recordsScanned = snapshot.Sections.Sum(section => section.Rows.Count);
        var blocked = proposals.Count(proposal => proposal.Policy.Status == HubSpotPolicyStatus.Blocked);
        var needsReview = proposals.Count(proposal => proposal.Policy.Status == HubSpotPolicyStatus.NeedsHumanDecision);
        var actionable = proposals.Count(proposal => proposal.Policy.Status == HubSpotPolicyStatus.AllowedForReview);

        var statusDetail = proposals.Count == 0
            ? "No workflow proposals were generated from the current HubSpot snapshot."
            : "The agent found useful CRM handoff work, but V1 keeps every write behind review.";

        return new HubSpotQualitySummary
        {
            StatusLabel = blocked > 0 || needsReview > 0
                ? "Useful, but not trusted for writeback"
                : "Useful for review",
            StatusDetail = statusDetail,
            RecordsScanned = recordsScanned,
            ActionableProposals = actionable,
            NeedsHumanReview = needsReview,
            BlockedProposals = blocked,
            UnsafeWrites = proposals.Count(proposal => proposal.Policy.CanExecuteWriteback),
        };
    }
}

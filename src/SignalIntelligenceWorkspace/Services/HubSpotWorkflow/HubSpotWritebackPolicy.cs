using SignalIntelligenceWorkspace.Models.HubSpotWorkflow;

namespace SignalIntelligenceWorkspace.Services.HubSpotWorkflow;

public sealed class HubSpotWritebackPolicy
{
    public HubSpotPolicyDecision Evaluate(HubSpotProposalKind kind, IReadOnlyCollection<string> evidenceGaps)
    {
        if (kind is HubSpotProposalKind.ReviewOwnerAssignment or HubSpotProposalKind.ReviewStaleDeal)
        {
            return new HubSpotPolicyDecision
            {
                Status = HubSpotPolicyStatus.Blocked,
                Label = "Blocked",
                Reason = "Direct CRM writeback is disabled for V1; this action needs explicit owner and human confirmation.",
                CanExecuteWriteback = false,
            };
        }

        if (evidenceGaps.Count > 0 || kind is HubSpotProposalKind.CleanContactFields or HubSpotProposalKind.ReviewLifecycleStage)
        {
            return new HubSpotPolicyDecision
            {
                Status = HubSpotPolicyStatus.NeedsHumanDecision,
                Label = "Needs human decision",
                Reason = "The agent can propose the cleanup, but a human must confirm the CRM truth before any writeback.",
                CanExecuteWriteback = false,
            };
        }

        return new HubSpotPolicyDecision
        {
            Status = HubSpotPolicyStatus.AllowedForReview,
            Label = "Allowed for review",
            Reason = "This is safe to review as a draft memo. V1 still does not execute HubSpot writes.",
            CanExecuteWriteback = false,
        };
    }
}

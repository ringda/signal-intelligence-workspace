using SignalIntelligenceWorkspace.Models.HubSpot;
using SignalIntelligenceWorkspace.Models.HubSpotWorkflow;

namespace SignalIntelligenceWorkspace.Services.HubSpotWorkflow;

public sealed class HubSpotReadinessRules
{
    public HubSpotProposalKind KindForContact(HubSpotReadinessCard card)
    {
        if (card.Missing.Contains("owner"))
        {
            return HubSpotProposalKind.ReviewOwnerAssignment;
        }

        if (card.Missing.Contains("lifecycle stage"))
        {
            return HubSpotProposalKind.ReviewLifecycleStage;
        }

        return card.Missing.Count > 0
            ? HubSpotProposalKind.CleanContactFields
            : HubSpotProposalKind.DraftNextTouchMemo;
    }

    public HubSpotProposalPriority PriorityFor(HubSpotPolicyDecision policy) => policy.Status switch
    {
        HubSpotPolicyStatus.AllowedForReview => HubSpotProposalPriority.Good,
        HubSpotPolicyStatus.Blocked => HubSpotProposalPriority.Blocked,
        _ => HubSpotProposalPriority.Unclear,
    };

    public bool IsDealReviewCandidate(HubSpotCrmRow row)
    {
        var missingStage = row.Status.Contains("No stage", StringComparison.OrdinalIgnoreCase);
        var missingCloseDate = row.Detail.Contains("No close date", StringComparison.OrdinalIgnoreCase);
        var staleOrPastClose = row.Detail.Contains("Close", StringComparison.OrdinalIgnoreCase)
            && TryParseDisplayDate(row.Detail.Replace("Close", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(), out var closeDate)
            && closeDate.Date < DateTimeOffset.Now.Date;

        return missingStage || missingCloseDate || staleOrPastClose;
    }

    private static bool TryParseDisplayDate(string raw, out DateTimeOffset date) =>
        DateTimeOffset.TryParse(raw, out date);
}

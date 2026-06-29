using SignalIntelligenceWorkspace.Models.HubSpot;
using SignalIntelligenceWorkspace.Models.HubSpotWorkflow;

namespace SignalIntelligenceWorkspace.Services.HubSpotWorkflow;

public sealed class HubSpotProposalBuilder(
    HubSpotReadinessRules rules,
    HubSpotWritebackPolicy policy)
{
    public IReadOnlyList<HubSpotActionProposal> Build(HubSpotCrmSnapshot snapshot)
    {
        var proposals = new List<HubSpotActionProposal>();

        foreach (var card in snapshot.ReadinessCards)
        {
            var kind = rules.KindForContact(card);
            var decision = policy.Evaluate(kind, card.Missing);
            proposals.Add(BuildContactProposal(card, kind, decision));
        }

        var dealSection = snapshot.Sections.FirstOrDefault(section => section.Title == "Deals");
        if (dealSection is not null)
        {
            foreach (var deal in dealSection.Rows.Where(rules.IsDealReviewCandidate))
            {
                var decision = policy.Evaluate(HubSpotProposalKind.ReviewStaleDeal, ["deal timing or stage"]);
                proposals.Add(BuildDealProposal(deal, decision));
            }
        }

        return proposals
            .OrderBy(proposal => proposal.Priority)
            .ThenBy(proposal => proposal.RecordName)
            .ToList();
    }

    private HubSpotActionProposal BuildContactProposal(
        HubSpotReadinessCard card,
        HubSpotProposalKind kind,
        HubSpotPolicyDecision decision)
    {
        var missing = card.Missing.Count == 0 ? "none" : string.Join(", ", card.Missing);
        var evidence =
            new[]
            {
                $"Lifecycle: {card.Lifecycle}",
                $"Owner: {card.Owner}",
                $"Company: {card.Company}",
                $"Title: {card.Title}",
                $"Missing fields: {missing}",
            };

        return new HubSpotActionProposal
        {
            Id = $"contact-{card.Id}-{kind}",
            Kind = kind,
            Priority = rules.PriorityFor(decision),
            ObjectType = "contact",
            ObjectId = card.Id,
            RecordName = card.Name,
            Title = TitleFor(kind, card.Name),
            Summary = card.Why,
            EvidenceSummary = $"Score {card.Score}/100; missing {missing}.",
            Evidence = evidence,
            ProposedAction = card.NextAction,
            SuggestedNoteBody = card.SuggestedNoteBody,
            Policy = decision,
            HubSpotUrl = card.HubSpotUrl,
        };
    }

    private HubSpotActionProposal BuildDealProposal(HubSpotCrmRow deal, HubSpotPolicyDecision decision)
    {
        return new HubSpotActionProposal
        {
            Id = $"deal-{deal.Id}-ReviewStaleDeal",
            Kind = HubSpotProposalKind.ReviewStaleDeal,
            Priority = rules.PriorityFor(decision),
            ObjectType = "deal",
            ObjectId = deal.Id,
            RecordName = deal.Primary,
            Title = $"Review deal hygiene for {deal.Primary}",
            Summary = "Deal timing or stage needs human review before CRM cleanup.",
            EvidenceSummary = $"{deal.Status}; {deal.Detail}; updated {deal.UpdatedAt}.",
            Evidence =
            [
                $"Stage: {deal.Status}",
                $"Timing: {deal.Detail}",
                $"Amount: {deal.Secondary}",
                $"Updated: {deal.UpdatedAt}",
            ],
            ProposedAction = "Review deal stage, close date, and next step before any CRM writeback.",
            SuggestedNoteBody =
                $"CRM deal hygiene review{Environment.NewLine}" +
                $"Deal: {deal.Primary}{Environment.NewLine}" +
                $"Stage: {deal.Status}{Environment.NewLine}" +
                $"Detail: {deal.Detail}{Environment.NewLine}" +
                "Recommended next action: confirm stage and timing with the owner before updating HubSpot.",
            Policy = decision,
            HubSpotUrl = deal.HubSpotUrl,
        };
    }

    private static string TitleFor(HubSpotProposalKind kind, string name) => kind switch
    {
        HubSpotProposalKind.ReviewOwnerAssignment => $"Confirm owner before follow-up for {name}",
        HubSpotProposalKind.ReviewLifecycleStage => $"Confirm lifecycle stage for {name}",
        HubSpotProposalKind.CleanContactFields => $"Clean CRM handoff fields for {name}",
        HubSpotProposalKind.DraftNextTouchMemo => $"Draft next-touch memo for {name}",
        _ => $"Review CRM handoff for {name}",
    };
}

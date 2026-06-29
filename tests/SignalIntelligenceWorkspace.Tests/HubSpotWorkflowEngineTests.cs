using SignalIntelligenceWorkspace.Models.HubSpot;
using SignalIntelligenceWorkspace.Models.HubSpotWorkflow;
using SignalIntelligenceWorkspace.Services.HubSpotWorkflow;

namespace SignalIntelligenceWorkspace.Tests;

public sealed class HubSpotWorkflowEngineTests
{
    [Fact]
    public void Contact_without_owner_creates_blocked_owner_review()
    {
        var run = Engine().Run(Snapshot(cards: [Card("1", missing: ["owner"])]));

        var proposal = Assert.Single(run.Proposals);
        Assert.Equal(HubSpotProposalKind.ReviewOwnerAssignment, proposal.Kind);
        Assert.Equal(HubSpotPolicyStatus.Blocked, proposal.Policy.Status);
        Assert.False(proposal.Policy.CanExecuteWriteback);
        Assert.Equal(1, run.Quality.BlockedProposals);
        Assert.Equal(0, run.Quality.UnsafeWrites);
    }

    [Fact]
    public void Contact_missing_lifecycle_company_or_title_increases_human_review_count()
    {
        var run = Engine().Run(Snapshot(cards: [Card("2", missing: ["lifecycle stage", "company", "job title"])]));

        var proposal = Assert.Single(run.Proposals);
        Assert.Equal(HubSpotProposalKind.ReviewLifecycleStage, proposal.Kind);
        Assert.Equal(HubSpotPolicyStatus.NeedsHumanDecision, proposal.Policy.Status);
        Assert.Equal(1, run.Quality.NeedsHumanReview);
    }

    [Fact]
    public void Clean_contact_creates_next_touch_memo_for_review_only()
    {
        var run = Engine().Run(Snapshot(cards: [Card("3", missing: [])]));

        var proposal = Assert.Single(run.Proposals);
        Assert.Equal(HubSpotProposalKind.DraftNextTouchMemo, proposal.Kind);
        Assert.Equal(HubSpotPolicyStatus.AllowedForReview, proposal.Policy.Status);
        Assert.False(proposal.Policy.CanExecuteWriteback);
        Assert.Equal(1, run.Quality.ActionableProposals);
    }

    [Fact]
    public void Deal_without_stage_or_close_date_creates_stale_deal_review()
    {
        var run = Engine().Run(Snapshot(
            cards: [],
            deals: [Deal("7", status: "No stage", detail: "No close date")]));

        var proposal = Assert.Single(run.Proposals);
        Assert.Equal(HubSpotProposalKind.ReviewStaleDeal, proposal.Kind);
        Assert.Equal(HubSpotPolicyStatus.Blocked, proposal.Policy.Status);
        Assert.Equal("deal", proposal.ObjectType);
    }

    [Fact]
    public void Audit_events_are_append_only_newest_first_and_reference_policy_decisions()
    {
        var run = Engine().Run(Snapshot(cards:
        [
            Card("1", missing: ["owner"]),
            Card("2", missing: []),
        ]));

        Assert.Equal(2, run.AuditEvents.Count);
        Assert.Equal("0002", run.AuditEvents[0].Id);
        Assert.Equal("0001", run.AuditEvents[1].Id);
        Assert.All(run.AuditEvents, e => Assert.Equal("ProposalGenerated", e.EventType));
        Assert.Contains(run.AuditEvents, e => e.Decision == "Blocked");
        Assert.Contains(run.AuditEvents, e => e.Decision == "Allowed for review");
    }

    private static HubSpotWorkflowEngine Engine()
    {
        var rules = new HubSpotReadinessRules();
        var policy = new HubSpotWritebackPolicy();
        var builder = new HubSpotProposalBuilder(rules, policy);
        return new HubSpotWorkflowEngine(builder, new HubSpotWorkflowAuditLog(), TimeProvider.System);
    }

    private static HubSpotCrmSnapshot Snapshot(
        IReadOnlyList<HubSpotReadinessCard> cards,
        IReadOnlyList<HubSpotCrmRow>? deals = null)
    {
        return new HubSpotCrmSnapshot
        {
            RefreshedAt = DateTimeOffset.Parse("2026-06-25T12:00:00+08:00"),
            PortalUrl = "https://app.hubspot.test/contacts/1",
            ReadinessCards = cards,
            Sections =
            [
                new HubSpotCrmSection
                {
                    Title = "Contacts",
                    Rows = cards.Select(card => new HubSpotCrmRow
                    {
                        Id = card.Id,
                        Primary = card.Name,
                        Secondary = card.Email,
                        Status = card.Lifecycle,
                        Detail = card.Company,
                        UpdatedAt = "Jun 25, 2026",
                        HubSpotUrl = card.HubSpotUrl,
                    }).ToList(),
                },
                new HubSpotCrmSection
                {
                    Title = "Deals",
                    Rows = deals ?? [],
                },
            ],
        };
    }

    private static HubSpotReadinessCard Card(string id, IReadOnlyList<string> missing)
    {
        return new HubSpotReadinessCard
        {
            Id = id,
            Name = $"Contact {id}",
            Email = $"contact{id}@example.com",
            Company = missing.Contains("company") ? "-" : "Acme",
            Title = missing.Contains("job title") ? "-" : "Director",
            Lifecycle = missing.Contains("lifecycle stage") ? "No lifecycle" : "opportunity",
            Owner = missing.Contains("owner") ? "No owner" : "Avery Owner",
            Score = missing.Count == 0 ? 100 : 70,
            Status = missing.Count == 0 ? "Ready" : "Needs Cleanup",
            Missing = missing,
            NextAction = "Prepare a governed next action.",
            Why = "Evidence supports a reviewable CRM action.",
            SuggestedTaskTitle = $"Review Contact {id}",
            SuggestedNoteBody = "Suggested note",
            UpdatedAt = "Jun 25, 2026",
            HubSpotUrl = $"https://app.hubspot.test/contact/{id}",
        };
    }

    private static HubSpotCrmRow Deal(string id, string status, string detail)
    {
        return new HubSpotCrmRow
        {
            Id = id,
            Primary = $"Deal {id}",
            Secondary = "$1000",
            Status = status,
            Detail = detail,
            UpdatedAt = "Jun 25, 2026",
            HubSpotUrl = $"https://app.hubspot.test/deal/{id}",
        };
    }
}

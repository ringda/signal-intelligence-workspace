using SignalIntelligenceWorkspace.Models;

namespace SignalIntelligenceWorkspace.Services.Scenarios;

/// <summary>
/// First scenario pack: a transportation-consulting proposal and market-intelligence world.
/// All clients, projects, and people are fictional. The scenario id is internal only and
/// is never rendered by the UI.
/// </summary>
public static class DksProposalScenario
{
    public static ScenarioPack Create() => new(
        Id: "dks-proposal",
        DisplayTitle: "Transportation Consulting — Proposal & Market Intelligence Scenario",
        Segments:
        [
            "Transit Planning",
            "Traffic Operations",
            "Active Transportation",
            "Safety Studies",
            "Smart Mobility",
        ],
        ResourceTypes:
        [
            "Past Proposal",
            "Project Description",
            "Staff Resume",
            "Project Imagery",
            "Feedback Note",
            "Market Signal",
        ],
        Resources:
        [
            new ResourceRow { Id = "R-001", ResourceType = "Past Proposal", Title = "Riverbend BRT Corridor Proposal", ClientProject = "Riverbend Transit Authority", MarketSegment = "Transit Planning", Status = "Active", Owner = "Mara Quinn", LastUpdated = new DateOnly(2026, 6, 2), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-002", ResourceType = "Past Proposal", Title = "Northgate Signal Retiming Proposal", ClientProject = "Northgate County DOT", MarketSegment = "Traffic Operations", Status = "Active", Owner = "Devon Park", LastUpdated = new DateOnly(2026, 5, 21), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-003", ResourceType = "Past Proposal", Title = "Harborview Safe Routes Proposal", ClientProject = "Harborview City Council", MarketSegment = "Safety Studies", Status = "Archived", Owner = "Lena Osei", LastUpdated = new DateOnly(2026, 4, 14), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-004", ResourceType = "Project Description", Title = "Cascadia Metro Bus Network Redesign", ClientProject = "Cascadia Metro District", MarketSegment = "Transit Planning", Status = "Active", Owner = "Mara Quinn", LastUpdated = new DateOnly(2026, 5, 30), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-005", ResourceType = "Project Description", Title = "Westfall Adaptive Signal Pilot", ClientProject = "Westfall Regional MPO", MarketSegment = "Traffic Operations", Status = "In Progress", Owner = "Devon Park", LastUpdated = new DateOnly(2026, 6, 8), ReviewState = ReviewState.NeedsReview },
            new ResourceRow { Id = "R-006", ResourceType = "Project Description", Title = "Maple Junction Greenway Phase 2", ClientProject = "Maple Junction Public Works", MarketSegment = "Active Transportation", Status = "Active", Owner = "Priya Nair", LastUpdated = new DateOnly(2026, 5, 27), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-007", ResourceType = "Staff Resume", Title = "Senior Transit Planner Resume", ClientProject = "Internal", MarketSegment = "Transit Planning", Status = "Active", Owner = "Tom Calloway", LastUpdated = new DateOnly(2026, 6, 5), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-008", ResourceType = "Staff Resume", Title = "Traffic Operations Lead Resume", ClientProject = "Internal", MarketSegment = "Traffic Operations", Status = "Active", Owner = "Tom Calloway", LastUpdated = new DateOnly(2026, 6, 5), ReviewState = ReviewState.NeedsReview },
            new ResourceRow { Id = "R-009", ResourceType = "Staff Resume", Title = "Active Transportation Specialist Resume", ClientProject = "Internal", MarketSegment = "Active Transportation", Status = "Active", Owner = "Tom Calloway", LastUpdated = new DateOnly(2026, 5, 19), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-010", ResourceType = "Project Imagery", Title = "Riverbend Station Rendering Set", ClientProject = "Riverbend Transit Authority", MarketSegment = "Transit Planning", Status = "Active", Owner = "Jordan Hale", LastUpdated = new DateOnly(2026, 5, 12), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-011", ResourceType = "Project Imagery", Title = "Greenway Before/After Photo Series", ClientProject = "Maple Junction Public Works", MarketSegment = "Active Transportation", Status = "Active", Owner = "Jordan Hale", LastUpdated = new DateOnly(2026, 6, 1), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-012", ResourceType = "Feedback Note", Title = "Debrief: Riverbend BRT Shortlist Interview", ClientProject = "Riverbend Transit Authority", MarketSegment = "Transit Planning", Status = "Active", Owner = "Mara Quinn", LastUpdated = new DateOnly(2026, 6, 9), ReviewState = ReviewState.NeedsReview },
            new ResourceRow { Id = "R-013", ResourceType = "Feedback Note", Title = "Client Survey: Northgate Signal Program", ClientProject = "Northgate County DOT", MarketSegment = "Traffic Operations", Status = "Active", Owner = "Devon Park", LastUpdated = new DateOnly(2026, 5, 25), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-014", ResourceType = "Feedback Note", Title = "Post-Project Review: Harborview Audits", ClientProject = "Harborview City Council", MarketSegment = "Safety Studies", Status = "Archived", Owner = "Lena Osei", LastUpdated = new DateOnly(2026, 4, 28), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-015", ResourceType = "Market Signal", Title = "Eastline Port Access Study RFP Watch", ClientProject = "Eastline Port Authority", MarketSegment = "Traffic Operations", Status = "In Progress", Owner = "Priya Nair", LastUpdated = new DateOnly(2026, 6, 10), ReviewState = ReviewState.NeedsReview },
            new ResourceRow { Id = "R-016", ResourceType = "Market Signal", Title = "Silver Basin Microtransit Grant Announcement", ClientProject = "Silver Basin Transit", MarketSegment = "Smart Mobility", Status = "Active", Owner = "Priya Nair", LastUpdated = new DateOnly(2026, 6, 7), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-017", ResourceType = "Market Signal", Title = "Statewide Vision Zero Funding Cycle", ClientProject = "Multiple", MarketSegment = "Safety Studies", Status = "Active", Owner = "Lena Osei", LastUpdated = new DateOnly(2026, 6, 4), ReviewState = ReviewState.Approved },
            new ResourceRow { Id = "R-018", ResourceType = "Past Proposal", Title = "Silver Basin Curb Data Pilot Proposal", ClientProject = "Silver Basin Transit", MarketSegment = "Smart Mobility", Status = "Archived", Owner = "Devon Park", LastUpdated = new DateOnly(2026, 3, 31), ReviewState = ReviewState.Approved },
        ],
        Themes:
        [
            new ThemeRow { Id = "T-01", Theme = "Transit signal priority corridors", Segment = "Transit Planning", Frequency = 9, Confidence = Confidence.High, Example = "Agencies keep asking whether signal priority can be bundled into bus corridor upgrades.", SuggestedUse = "Lead with bundled corridor + signal priority scope in transit proposals.", ReviewStatus = ReviewState.Approved },
            new ThemeRow { Id = "T-02", Theme = "Quick-build intersection retrofits", Segment = "Traffic Operations", Frequency = 8, Confidence = Confidence.High, Example = "Clients want visible safety fixes delivered within a single budget year.", SuggestedUse = "Offer a quick-build delivery track in operations proposals.", ReviewStatus = ReviewState.Approved },
            new ThemeRow { Id = "T-03", Theme = "School-zone safety audits", Segment = "Safety Studies", Frequency = 7, Confidence = Confidence.High, Example = "Three districts requested walk audits around elementary schools this quarter.", SuggestedUse = "Package school-zone audits as a standard study module.", ReviewStatus = ReviewState.NeedsReview },
            new ThemeRow { Id = "T-04", Theme = "Protected bike lane expansions", Segment = "Active Transportation", Frequency = 6, Confidence = Confidence.Medium, Example = "Council members cite resident demand for protected lanes on arterials.", SuggestedUse = "Pair lane expansion concepts with safety data storytelling.", ReviewStatus = ReviewState.Approved },
            new ThemeRow { Id = "T-05", Theme = "Trail network gap closures", Segment = "Active Transportation", Frequency = 5, Confidence = Confidence.High, Example = "Grant programs favor projects that close named trail gaps.", SuggestedUse = "Map fundable gap segments before the next grant cycle.", ReviewStatus = ReviewState.Approved },
            new ThemeRow { Id = "T-06", Theme = "Adaptive signal timing requests", Segment = "Traffic Operations", Frequency = 5, Confidence = Confidence.Medium, Example = "Operations staff ask if adaptive timing can cut peak-hour complaints.", SuggestedUse = "Include adaptive-timing pilot options with measurable KPIs.", ReviewStatus = ReviewState.Approved },
            new ThemeRow { Id = "T-07", Theme = "Curbside management pilots", Segment = "Smart Mobility", Frequency = 4, Confidence = Confidence.Medium, Example = "Downtown groups want delivery-zone pilots before regulating curb use.", SuggestedUse = "Position curb pilots as low-risk data-first engagements.", ReviewStatus = ReviewState.NeedsReview },
            new ThemeRow { Id = "T-08", Theme = "Micromobility data dashboards", Segment = "Smart Mobility", Frequency = 3, Confidence = Confidence.Low, Example = "One agency asked about scooter trip dashboards during a debrief.", SuggestedUse = "Track interest; not yet strong enough to lead a proposal.", ReviewStatus = ReviewState.Drafted },
            new ThemeRow { Id = "T-09", Theme = "Bus stop accessibility upgrades", Segment = "Transit Planning", Frequency = 4, Confidence = Confidence.Medium, Example = "Riders flag inaccessible stops in nearly every public comment round.", SuggestedUse = "Add accessibility audit add-ons to transit planning scopes.", ReviewStatus = ReviewState.Approved },
            new ThemeRow { Id = "T-10", Theme = "Vision Zero corridor studies", Segment = "Safety Studies", Frequency = 3, Confidence = Confidence.High, Example = "New state funding cycle explicitly references Vision Zero corridors.", SuggestedUse = "Align safety study language with the funding cycle terminology.", ReviewStatus = ReviewState.Approved },
        ],
        DemoPrompts:
        [
            "Summarize recurring customer feedback themes for the proposal team.",
            "Show high-confidence market opportunity signals.",
            "Draft a short insight memo for active transportation proposal planning.",
        ]);
}

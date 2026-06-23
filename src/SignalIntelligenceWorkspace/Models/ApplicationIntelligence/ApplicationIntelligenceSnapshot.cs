namespace SignalIntelligenceWorkspace.Models.ApplicationIntelligence;

public sealed class ApplicationIntelligenceSnapshot
{
    public ApplicationIntelligenceSummary Summary { get; set; } = new();
    public List<ApplicationIntelligenceFitSegment> FitSegments { get; set; } = [];
    public List<ApplicationIntelligenceCaseRecord> CaseRecords { get; set; } = [];
    public DateTimeOffset LoadedAt { get; set; }
}

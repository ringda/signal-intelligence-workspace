namespace SignalIntelligenceWorkspace.Models.ApplicationIntelligence;

public sealed class ApplicationIntelligenceFitSegment
{
    public string SegmentKey { get; set; } = string.Empty;
    public int Count { get; set; }
    public int AverageReadiness { get; set; }
}

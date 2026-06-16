namespace SignalIntelligenceWorkspace.Models.Cockpit;

public sealed class CockpitDailySeries
{
    public DateTime Day { get; set; }
    public int JdScanned { get; set; }
    public int JdScannedTarget { get; set; }
    public int JdCorrectPool { get; set; }
    public int ApplicationsApplied { get; set; }
    public int ApplicationsAppliedTarget { get; set; }
}

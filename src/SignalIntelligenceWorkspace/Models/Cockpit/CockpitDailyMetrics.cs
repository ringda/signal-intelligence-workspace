namespace SignalIntelligenceWorkspace.Models.Cockpit;

public sealed class CockpitDailyMetrics
{
    public long JdScanned24h { get; set; }
    public int JdScannedTarget { get; set; }
    public long JdCorrectPool24h { get; set; }
    public long JdScannedWeek { get; set; }
    public long ApplicationsAppliedToday { get; set; }
    public int ApplicationsAppliedTarget { get; set; }
    public long ApplicationsAppliedWeek { get; set; }
    public int ApplicationsAppliedWeekTarget { get; set; }
    public long JdScannedToday { get; set; }
    public long JdCorrectPoolToday { get; set; }
    public long ApplicationsRoleBriefedToday { get; set; }
    public long ApplicationsResumeCustomizedToday { get; set; }
    public long ApplicationsNetworkingStartedToday { get; set; }
    public DateOnly LocalToday { get; set; }
    public DateOnly LocalWeekStart { get; set; }
    public string MetricTimezone { get; set; } = string.Empty;
}

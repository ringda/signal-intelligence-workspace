namespace SignalIntelligenceWorkspace.Models.ApplicationIntelligence;

public sealed class ApplicationIntelligenceSummary
{
    public long JobsScreenedTotal { get; set; }
    public long JobsScreened30Days { get; set; }
    public long JobsWithDescriptions { get; set; }
    public long QualifiedRoles { get; set; }
    public long ApplicationRows { get; set; }
    public long ApplicationsSubmitted { get; set; }
    public long RoleBriefed { get; set; }
    public long Tailored { get; set; }
    public long NetworkingStarted { get; set; }
    public long TrackerWritten { get; set; }
    public long LearningSignals { get; set; }
    public DateOnly LocalToday { get; set; }
    public string MetricTimezone { get; set; } = string.Empty;

    public long CompletedWorkflowSteps =>
        RoleBriefed + Tailored + ApplicationsSubmitted + NetworkingStarted + TrackerWritten + LearningSignals;
}

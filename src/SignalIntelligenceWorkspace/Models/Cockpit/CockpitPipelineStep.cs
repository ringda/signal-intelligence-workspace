namespace SignalIntelligenceWorkspace.Models.Cockpit;

public sealed class CockpitPipelineStep
{
    public int StepOrder { get; set; }
    public string StepKey { get; set; } = string.Empty;
    public string LabelZh { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public int ActualCount { get; set; }
    public int TargetCount { get; set; }
    public string StepStatus { get; set; } = string.Empty;
    public string SourceNote { get; set; } = string.Empty;
}

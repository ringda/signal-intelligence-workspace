namespace SignalIntelligenceWorkspace.Models.Cockpit;

public sealed class CockpitJobProcess
{
    public string JobId { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string? StatusDetail { get; set; }
    public string DispositionState { get; set; } = string.Empty;
    public string? DispositionNote { get; set; }
    public string? MatchLevel { get; set; }
    public string? WinReasoning { get; set; }
    public int? WinScore { get; set; }
    public string? AiReason { get; set; }
    public string? AiSummary { get; set; }
    public string? VisaNote { get; set; }
    public string? JdFullText { get; set; }
    public string? PipelineFolder { get; set; }
    public string? PjdRoleSummary { get; set; }
    public string? PjdHiringFocus { get; set; }
}

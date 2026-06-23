namespace SignalIntelligenceWorkspace.Models.ApplicationIntelligence;

public sealed class ApplicationIntelligenceCaseRecord
{
    public string JobId { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string DispositionState { get; set; } = string.Empty;
    public string? MatchLevel { get; set; }
    public int? MatchScore { get; set; }
    public int? WinScore { get; set; }
    public string? JudgementSummary { get; set; }
    public string? JdExcerpt { get; set; }
    public string? PipelineFolder { get; set; }
    public string PjdStatus { get; set; } = string.Empty;
    public string PtStatus { get; set; } = string.Empty;
    public string PrpStatus { get; set; } = string.Empty;
    public string PaStatus { get; set; } = string.Empty;
    public string NetworkingStage { get; set; } = string.Empty;
    public string? PjdRoleSummary { get; set; }
    public string? PjdHiringFocus { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

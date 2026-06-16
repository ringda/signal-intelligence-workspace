namespace SignalIntelligenceWorkspace.Models.Cockpit;

public sealed class CockpitRecentJob
{
    public string JobId { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string DispositionState { get; set; } = string.Empty;
    public string? MatchLevel { get; set; }
    public int? MatchScore { get; set; }
    public int? WinScore { get; set; }
    public string JudgementSummary { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string? PipelineFolder { get; set; }
}

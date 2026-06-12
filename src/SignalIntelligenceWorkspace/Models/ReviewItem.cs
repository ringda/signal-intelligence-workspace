namespace SignalIntelligenceWorkspace.Models;

public sealed class ReviewItem
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Body { get; init; }
    public required string SourceCommand { get; init; }
    public required string CreatedAt { get; init; }
    public ReviewState State { get; set; } = ReviewState.Drafted;
}

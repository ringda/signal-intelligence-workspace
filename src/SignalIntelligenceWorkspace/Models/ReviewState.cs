namespace SignalIntelligenceWorkspace.Models;

public enum ReviewState
{
    Drafted,
    NeedsReview,
    Approved,
    Rejected,
}

public enum Confidence
{
    High,
    Medium,
    Low,
}

public static class ReviewStateExtensions
{
    public static string ToDisplay(this ReviewState state) => state switch
    {
        ReviewState.NeedsReview => "Needs Review",
        _ => state.ToString(),
    };
}

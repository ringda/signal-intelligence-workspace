namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public sealed record PublicFeedbackRecord(
    string Id,
    DateTimeOffset SubmittedAt,
    string FeedbackType,
    string Message,
    string PagePath);

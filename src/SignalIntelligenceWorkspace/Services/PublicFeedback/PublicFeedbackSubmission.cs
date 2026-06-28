namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public sealed record PublicFeedbackSubmission(
    string FeedbackType,
    string Message,
    string PagePath);

namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public sealed record PublicFeedbackReceipt(
    string Id,
    DateTimeOffset SubmittedAt);

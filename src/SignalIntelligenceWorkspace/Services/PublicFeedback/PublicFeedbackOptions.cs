namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public sealed class PublicFeedbackOptions
{
    public string InboxPath { get; set; } = Path.Combine("App_Data", "public-feedback.jsonl");
}

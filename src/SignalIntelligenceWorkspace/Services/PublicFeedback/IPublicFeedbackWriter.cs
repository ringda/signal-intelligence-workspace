namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public interface IPublicFeedbackWriter
{
    Task WriteAsync(PublicFeedbackRecord record, CancellationToken cancellationToken = default);
}

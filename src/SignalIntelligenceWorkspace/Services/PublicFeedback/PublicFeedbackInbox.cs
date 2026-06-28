namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public sealed class PublicFeedbackInbox
{
    private const int MaxMessageLength = 600;
    private static readonly HashSet<string> AllowedFeedbackTypes = new(StringComparer.Ordinal)
    {
        "most-useful",
        "least-relevant",
        "best-fit-team"
    };

    private readonly TimeProvider timeProvider;
    private readonly IPublicFeedbackWriter writer;

    public PublicFeedbackInbox(
        TimeProvider timeProvider,
        IPublicFeedbackWriter writer)
    {
        this.timeProvider = timeProvider;
        this.writer = writer;
    }

    public async Task<PublicFeedbackReceipt> SubmitAsync(
        PublicFeedbackSubmission submission,
        CancellationToken cancellationToken = default)
    {
        var feedbackType = NormalizeFeedbackType(submission.FeedbackType);
        var message = NormalizeMessage(submission.Message);
        var pagePath = string.IsNullOrWhiteSpace(submission.PagePath) ? "/" : submission.PagePath.Trim();
        var submittedAt = timeProvider.GetUtcNow();
        var receipt = new PublicFeedbackReceipt(CreateId(submittedAt), submittedAt);
        var record = new PublicFeedbackRecord(receipt.Id, submittedAt, feedbackType, message, pagePath);

        await writer.WriteAsync(record, cancellationToken);

        return receipt;
    }

    private static string NormalizeFeedbackType(string feedbackType)
    {
        var normalized = feedbackType.Trim();
        if (!AllowedFeedbackTypes.Contains(normalized))
        {
            throw new ArgumentException("Choose one feedback type.", nameof(feedbackType));
        }

        return normalized;
    }

    private static string NormalizeMessage(string message)
    {
        var normalized = message.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Add a short note first.", nameof(message));
        }

        if (normalized.Length > MaxMessageLength)
        {
            throw new ArgumentException("Keep the note under 600 characters.", nameof(message));
        }

        return normalized;
    }

    private static string CreateId(DateTimeOffset submittedAt)
    {
        return $"pf_{submittedAt:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}"[..31];
    }
}

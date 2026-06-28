using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SignalIntelligenceWorkspace.Services.PublicFeedback;

public sealed class PublicFeedbackInbox
{
    private const int MaxMessageLength = 600;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly SemaphoreSlim WriteLock = new(1, 1);
    private static readonly HashSet<string> AllowedFeedbackTypes = new(StringComparer.Ordinal)
    {
        "most-useful",
        "least-relevant",
        "best-fit-team"
    };

    private readonly IHostEnvironment environment;
    private readonly PublicFeedbackOptions options;
    private readonly TimeProvider timeProvider;

    public PublicFeedbackInbox(
        IHostEnvironment environment,
        IOptions<PublicFeedbackOptions> options,
        TimeProvider timeProvider)
    {
        this.environment = environment;
        this.options = options.Value;
        this.timeProvider = timeProvider;
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
        var line = JsonSerializer.Serialize(record, JsonOptions) + Environment.NewLine;
        var inboxPath = ResolveInboxPath();
        var directory = Path.GetDirectoryName(inboxPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(inboxPath, line, cancellationToken);
        }
        finally
        {
            WriteLock.Release();
        }

        return receipt;
    }

    private string ResolveInboxPath()
    {
        var configuredPath = string.IsNullOrWhiteSpace(options.InboxPath)
            ? Path.Combine("App_Data", "public-feedback.jsonl")
            : options.InboxPath;

        return Path.GetFullPath(configuredPath, environment.ContentRootPath);
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

    private sealed record PublicFeedbackRecord(
        string Id,
        DateTimeOffset SubmittedAt,
        string FeedbackType,
        string Message,
        string PagePath);
}

using SignalIntelligenceWorkspace.Services.PublicFeedback;

namespace SignalIntelligenceWorkspace.Tests;

public sealed class PublicFeedbackInboxTests
{
    [Fact]
    public async Task SubmitAsync_writes_public_feedback_record()
    {
        var writer = new RecordingFeedbackWriter();
        var inbox = CreateInbox(writer);

        var receipt = await inbox.SubmitAsync(new PublicFeedbackSubmission(
            "most-useful",
            "  The AI-assisted skill layer is the strongest part.  ",
            "/"));

        var record = Assert.Single(writer.Records);
        Assert.StartsWith("pf_20260628123456000_", receipt.Id);
        Assert.Equal(receipt.Id, record.Id);
        Assert.Equal(DateTimeOffset.Parse("2026-06-28T12:34:56+00:00"), record.SubmittedAt);
        Assert.Equal("most-useful", record.FeedbackType);
        Assert.Equal("The AI-assisted skill layer is the strongest part.", record.Message);
        Assert.Equal("/", record.PagePath);
    }

    [Fact]
    public async Task SubmitAsync_rejects_blank_feedback()
    {
        var writer = new RecordingFeedbackWriter();
        var inbox = CreateInbox(writer);

        await Assert.ThrowsAsync<ArgumentException>(() => inbox.SubmitAsync(new PublicFeedbackSubmission(
            "most-useful",
            "   ",
            "/")));

        Assert.Empty(writer.Records);
    }

    [Fact]
    public async Task SubmitAsync_rejects_message_over_limit()
    {
        var writer = new RecordingFeedbackWriter();
        var inbox = CreateInbox(writer);

        await Assert.ThrowsAsync<ArgumentException>(() => inbox.SubmitAsync(new PublicFeedbackSubmission(
            "best-fit-team",
            new string('x', 601),
            "/")));

        Assert.Empty(writer.Records);
    }

    [Fact]
    public async Task SubmitAsync_rejects_unknown_feedback_type()
    {
        var writer = new RecordingFeedbackWriter();
        var inbox = CreateInbox(writer);

        await Assert.ThrowsAsync<ArgumentException>(() => inbox.SubmitAsync(new PublicFeedbackSubmission(
            "email-me",
            "This should not become chart data.",
            "/")));

        Assert.Empty(writer.Records);
    }

    [Fact]
    public async Task SubmitAsync_propagates_writer_failure_after_validation()
    {
        var writer = new FailingFeedbackWriter();
        var inbox = CreateInbox(writer);

        await Assert.ThrowsAsync<InvalidOperationException>(() => inbox.SubmitAsync(new PublicFeedbackSubmission(
            "least-relevant",
            "The write path should surface a DB failure.",
            "/")));
    }

    [Fact]
    public void BuildSchemaSql_creates_feedback_table_and_indexes()
    {
        var sql = PublicFeedbackSchemaInitializer.BuildSchemaSql("public_feedback", "submissions");

        Assert.Contains("CREATE SCHEMA IF NOT EXISTS \"public_feedback\"", sql);
        Assert.Contains("CREATE TABLE IF NOT EXISTS \"public_feedback\".\"submissions\"", sql);
        Assert.Contains("id text PRIMARY KEY", sql);
        Assert.Contains("submitted_at timestamptz NOT NULL", sql);
        Assert.Contains("feedback_type text NOT NULL", sql);
        Assert.Contains("created_at timestamptz NOT NULL DEFAULT now()", sql);
        Assert.Contains("ON \"public_feedback\".\"submissions\" (submitted_at DESC)", sql);
        Assert.Contains("ON \"public_feedback\".\"submissions\" (feedback_type, submitted_at DESC)", sql);
    }

    [Fact]
    public void BuildSchemaSql_rejects_unsafe_identifier()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PublicFeedbackSchemaInitializer.BuildSchemaSql("public_feedback;drop", "submissions"));
    }

    private static PublicFeedbackInbox CreateInbox(IPublicFeedbackWriter writer)
    {
        return new PublicFeedbackInbox(
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-28T12:34:56+00:00")),
            writer);
    }

    private sealed class RecordingFeedbackWriter : IPublicFeedbackWriter
    {
        public List<PublicFeedbackRecord> Records { get; } = [];

        public Task WriteAsync(PublicFeedbackRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingFeedbackWriter : IPublicFeedbackWriter
    {
        public Task WriteAsync(PublicFeedbackRecord record, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Database write failed.");
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp;
    }
}

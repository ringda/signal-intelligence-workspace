using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SignalIntelligenceWorkspace.Services.PublicFeedback;

namespace SignalIntelligenceWorkspace.Tests;

public sealed class PublicFeedbackInboxTests
{
    [Fact]
    public async Task SubmitAsync_appends_public_feedback_record()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var inbox = CreateInbox(tempRoot);

            var receipt = await inbox.SubmitAsync(new PublicFeedbackSubmission(
                "most-useful",
                "The AI-assisted skill layer is the strongest part.",
                "/"));

            var inboxPath = Path.Combine(tempRoot, "App_Data", "public-feedback.jsonl");
            var line = Assert.Single(await File.ReadAllLinesAsync(inboxPath));
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            Assert.StartsWith("pf_20260628123456000_", receipt.Id);
            Assert.Equal(receipt.Id, root.GetProperty("id").GetString());
            Assert.Equal("most-useful", root.GetProperty("feedbackType").GetString());
            Assert.Equal("The AI-assisted skill layer is the strongest part.", root.GetProperty("message").GetString());
            Assert.Equal("/", root.GetProperty("pagePath").GetString());
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitAsync_rejects_blank_feedback()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var inbox = CreateInbox(tempRoot);

            await Assert.ThrowsAsync<ArgumentException>(() => inbox.SubmitAsync(new PublicFeedbackSubmission(
                "most-useful",
                "   ",
                "/")));

            Assert.False(File.Exists(Path.Combine(tempRoot, "App_Data", "public-feedback.jsonl")));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitAsync_rejects_message_over_limit()
    {
        var tempRoot = CreateTempRoot();
        try
        {
            var inbox = CreateInbox(tempRoot);

            await Assert.ThrowsAsync<ArgumentException>(() => inbox.SubmitAsync(new PublicFeedbackSubmission(
                "best-fit-team",
                new string('x', 601),
                "/")));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static PublicFeedbackInbox CreateInbox(string contentRoot)
    {
        return new PublicFeedbackInbox(
            new TestEnvironment(contentRoot),
            Options.Create(new PublicFeedbackOptions()),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-28T12:34:56+00:00")));
    }

    private static string CreateTempRoot()
    {
        return Directory.CreateTempSubdirectory("public-feedback-tests-").FullName;
    }

    private sealed class TestEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "SignalIntelligenceWorkspace.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FixedTimeProvider(DateTimeOffset timestamp) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => timestamp;
    }
}

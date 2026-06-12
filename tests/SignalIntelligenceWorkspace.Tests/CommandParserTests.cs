using SignalIntelligenceWorkspace.Models;
using SignalIntelligenceWorkspace.Services;

namespace SignalIntelligenceWorkspace.Tests;

public class CommandParserTests
{
    private static readonly string[] Segments =
    [
        "Transit Planning",
        "Traffic Operations",
        "Active Transportation",
        "Safety Studies",
        "Smart Mobility",
    ];

    private static ParseResult Parse(string prompt) => CommandParser.Parse(prompt, Segments);

    // --- The three fixed demo prompts must parse to their exact commands. ---

    [Fact]
    public void DemoPrompt1_SummarizeFeedback()
    {
        var result = Parse("Summarize recurring customer feedback themes for the proposal team.");

        Assert.Equal(ParseOutcome.Allowed, result.Outcome);
        var command = Assert.IsType<SummarizeFeedbackCommand>(result.Command);
        Assert.Equal("proposal team", command.Audience);
    }

    [Fact]
    public void DemoPrompt2_FilterGridHighConfidence()
    {
        var result = Parse("Show high-confidence market opportunity signals.");

        Assert.Equal(ParseOutcome.Allowed, result.Outcome);
        var command = Assert.IsType<FilterGridCommand>(result.Command);
        Assert.Equal("themes", command.Target);
        Assert.Equal("confidence", command.Field);
        Assert.Equal("High", command.Value);
    }

    [Fact]
    public void DemoPrompt3_DraftInsightMemo_ExtractsSegment()
    {
        var result = Parse("Draft a short insight memo for active transportation proposal planning.");

        Assert.Equal(ParseOutcome.Allowed, result.Outcome);
        var command = Assert.IsType<DraftInsightMemoCommand>(result.Command);
        Assert.Equal("Active Transportation", command.Segment);
    }

    [Theory]
    [InlineData("Summarize recurring customer feedback themes for the proposal team.")]
    [InlineData("Show high-confidence market opportunity signals.")]
    [InlineData("Draft a short insight memo for active transportation proposal planning.")]
    public void DemoPrompts_NeverTripForbiddenRules(string prompt)
    {
        var result = Parse(prompt);

        Assert.Equal(ParseOutcome.Allowed, result.Outcome);
    }

    // --- Every forbidden rule fires with a visible name and reason. ---

    [Theory]
    [InlineData("Delete all old proposals", "deleteData")]
    [InlineData("Email this summary to the client", "sendExternally")]
    [InlineData("Sync to the source system when done", "writeToSourceSystem")]
    [InlineData("Run a SQL query on the feedback table", "rawSql")]
    [InlineData("What is the API key for this workspace?", "getApiKey")]
    [InlineData("Run a script to clean things up", "executeCode")]
    public void ForbiddenPrompts_AreBlockedWithRuleName(string prompt, string expectedRule)
    {
        var result = Parse(prompt);

        Assert.Equal(ParseOutcome.Forbidden, result.Outcome);
        Assert.Equal(expectedRule, result.BlockedRuleName);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
        Assert.Null(result.Command);
    }

    // --- Deny-before-allow: a forbidden fragment poisons the whole prompt. ---

    [Fact]
    public void DenyBeforeAllow_DraftAndEmail_IsRejectedWhole()
    {
        var result = Parse("Draft a memo and email it to the team");

        Assert.Equal(ParseOutcome.Forbidden, result.Outcome);
        Assert.Equal("sendExternally", result.BlockedRuleName);
    }

    [Fact]
    public void DenyBeforeAllow_DeleteThenSummarize_IsRejectedWhole()
    {
        var result = Parse("Delete the old feedback and summarize the rest");

        Assert.Equal(ParseOutcome.Forbidden, result.Outcome);
        Assert.Equal("deleteData", result.BlockedRuleName);
    }

    // --- Graceful fallbacks. ---

    [Fact]
    public void UnrecognizedPrompt_FallsBackToUnmatched()
    {
        var result = Parse("What is the weather today?");

        Assert.Equal(ParseOutcome.Unmatched, result.Outcome);
        Assert.Null(result.Command);
        Assert.Contains("No safe command matched", result.Reason);
    }

    [Fact]
    public void EmptyPrompt_IsUnmatched()
    {
        var result = Parse("   ");

        Assert.Equal(ParseOutcome.Unmatched, result.Outcome);
    }

    // --- Secondary allow rules. ---

    [Fact]
    public void MediumConfidence_FilterIsSupported()
    {
        var result = Parse("Filter to medium-confidence signals");

        Assert.Equal(ParseOutcome.Allowed, result.Outcome);
        var command = Assert.IsType<FilterGridCommand>(result.Command);
        Assert.Equal("Medium", command.Value);
    }

    [Fact]
    public void CompareSegments_MentionedSegmentsAreExtracted()
    {
        var result = Parse("Compare the Transit Planning and Smart Mobility segments");

        Assert.Equal(ParseOutcome.Allowed, result.Outcome);
        var command = Assert.IsType<CompareMarketSegmentsCommand>(result.Command);
        Assert.Equal(["Transit Planning", "Smart Mobility"], command.Segments);
    }

    [Fact]
    public void DraftMemo_WithoutSegment_DefaultsToFirstSegment()
    {
        var result = Parse("Draft an insight memo");

        Assert.Equal(ParseOutcome.Allowed, result.Outcome);
        var command = Assert.IsType<DraftInsightMemoCommand>(result.Command);
        Assert.Equal("Transit Planning", command.Segment);
    }
}

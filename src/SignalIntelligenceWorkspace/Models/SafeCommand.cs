namespace SignalIntelligenceWorkspace.Models;

/// <summary>
/// The closed set of commands the assistant is allowed to propose.
/// Anything outside this whitelist is rejected before it reaches review.
/// </summary>
public abstract record SafeCommand(string Name);

public sealed record FilterGridCommand(string Target, string Field, string Value) : SafeCommand("filterGrid");

public sealed record DraftInsightMemoCommand(string Segment) : SafeCommand("draftInsightMemo");

public sealed record SummarizeFeedbackCommand(string Audience) : SafeCommand("summarizeFeedback");

public sealed record CompareMarketSegmentsCommand(IReadOnlyList<string> Segments) : SafeCommand("compareMarketSegments");

public sealed record MarkNeedsReviewCommand(string ItemRef) : SafeCommand("markNeedsReview");

public sealed record ApproveDraftCommand(string ItemRef) : SafeCommand("approveDraft");

public sealed record RejectDraftCommand(string ItemRef) : SafeCommand("rejectDraft");

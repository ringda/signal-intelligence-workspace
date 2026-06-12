using SignalIntelligenceWorkspace.Models;

namespace SignalIntelligenceWorkspace.Services;

/// <summary>
/// Per-circuit workspace state and the only writer of the audit log.
/// Governance invariant: every AI-initiated action is audited; manual grid
/// filtering/sorting never passes through here and is deliberately unaudited.
/// </summary>
public sealed class WorkspaceState
{
    public const string Role = "Analyst (demo user)";

    private readonly ScenarioPack _scenario;
    private readonly TimeProvider _time;
    private readonly List<ReviewItem> _reviewItems = [];
    private readonly List<AuditEntry> _auditLog = [];
    private int _nextId;

    public WorkspaceState(ScenarioPack scenario, TimeProvider time)
    {
        _scenario = scenario;
        _time = time;
    }

    public event Action? Changed;

    public ParseResult? Pending { get; private set; }

    public IReadOnlyList<ReviewItem> ReviewItems => _reviewItems;

    /// <summary>Newest first; entries are append-only and never mutated.</summary>
    public IReadOnlyList<AuditEntry> AuditLog => _auditLog;

    public string? AppliedThemeFilter { get; private set; }

    public IEnumerable<ThemeRow> FilteredThemes =>
        AppliedThemeFilter is null
            ? _scenario.Themes
            : _scenario.Themes.Where(t => t.Confidence.ToString() == AppliedThemeFilter);

    // Dashboard KPIs — computed live so approvals visibly move the numbers.
    public int ResourcesReady => _scenario.Resources.Count(r => r.ReviewState == ReviewState.Approved);

    public int ItemsNeedingReview =>
        _scenario.Resources.Count(r => r.ReviewState == ReviewState.NeedsReview)
        + _scenario.Themes.Count(t => t.ReviewStatus == ReviewState.NeedsReview)
        + DraftsAwaitingApproval;

    public int MarketSignalsThisWeek => _scenario.Themes.Sum(t => t.Frequency);

    public int FeedbackThemeCount => _scenario.Themes.Count;

    public int DraftsAwaitingApproval =>
        _reviewItems.Count(i => i.State is ReviewState.Drafted or ReviewState.NeedsReview);

    public ParseResult SubmitPrompt(string prompt)
    {
        var result = CommandParser.Parse(prompt, _scenario.Segments);
        switch (result.Outcome)
        {
            case ParseOutcome.Allowed:
                Pending = result;
                Append(result.RawPrompt, CommandFormatter.ToJson(result.Command!),
                    "Generated — awaiting review",
                    "Safe command generated. Nothing executes until a human approves.");
                break;
            case ParseOutcome.Forbidden:
                Pending = null;
                Append(result.RawPrompt, result.BlockedRuleName!,
                    "Auto-rejected (forbidden)", result.Reason!);
                break;
            default:
                Pending = null;
                Append(result.RawPrompt, "—", "No safe command matched", result.Reason!);
                break;
        }

        Notify();
        return result;
    }

    public void ApprovePending()
    {
        if (Pending?.Command is not { } command)
        {
            return;
        }

        Execute(command);
        Append(Pending.RawPrompt, CommandFormatter.ToJson(command), "Approved",
            "Command executed after human review.");
        Pending = null;
        Notify();
    }

    public void RejectPending()
    {
        if (Pending is null)
        {
            return;
        }

        var commandText = Pending.Command is null ? "—" : CommandFormatter.ToJson(Pending.Command);
        Append(Pending.RawPrompt, commandText, "Rejected",
            "Reviewer rejected the command. No state was changed.");
        Pending = null;
        Notify();
    }

    public void SetReviewState(string itemId, ReviewState state)
    {
        var item = _reviewItems.FirstOrDefault(i => i.Id == itemId);
        if (item is null)
        {
            return;
        }

        item.State = state;
        Append("—", "—", state.ToDisplay(), $"Reviewer set \"{item.Title}\" to {state.ToDisplay()}.");
        Notify();
    }

    public void ClearThemeFilter()
    {
        AppliedThemeFilter = null;
        Notify();
    }

    public void ResetSession()
    {
        _reviewItems.Clear();
        _auditLog.Clear();
        Pending = null;
        AppliedThemeFilter = null;
        _nextId = 0;
        Notify();
    }

    private void Execute(SafeCommand command)
    {
        switch (command)
        {
            case FilterGridCommand filter:
                AppliedThemeFilter = filter.Value;
                break;

            case SummarizeFeedbackCommand or DraftInsightMemoCommand or CompareMarketSegmentsCommand:
                var (title, body) = DraftComposer.Compose(command, _scenario);
                _reviewItems.Insert(0, new ReviewItem
                {
                    Id = NextId(),
                    Title = title,
                    Body = body,
                    SourceCommand = command.Name,
                    CreatedAt = Now(),
                });
                break;

            case MarkNeedsReviewCommand:
                var drafted = _reviewItems.FirstOrDefault(i => i.State == ReviewState.Drafted);
                if (drafted is not null)
                {
                    drafted.State = ReviewState.NeedsReview;
                }
                break;

            case ApproveDraftCommand:
                var toApprove = _reviewItems.FirstOrDefault(
                    i => i.State is ReviewState.Drafted or ReviewState.NeedsReview);
                if (toApprove is not null)
                {
                    toApprove.State = ReviewState.Approved;
                }
                break;

            case RejectDraftCommand:
                var toReject = _reviewItems.FirstOrDefault(
                    i => i.State is ReviewState.Drafted or ReviewState.NeedsReview);
                if (toReject is not null)
                {
                    toReject.State = ReviewState.Rejected;
                }
                break;
        }
    }

    private void Append(string prompt, string command, string decision, string reason) =>
        _auditLog.Insert(0, new AuditEntry(NextId(), Now(), Role, prompt, command, decision, reason));

    private string Now() => _time.GetLocalNow().ToString("HH:mm:ss");

    private string NextId() => (++_nextId).ToString("D4");

    private void Notify() => Changed?.Invoke();
}

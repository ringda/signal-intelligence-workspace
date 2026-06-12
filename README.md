# Signal Intelligence Workspace

A small **workflow-testing prototype** that turns messy business signals into searchable resources, reviewable AI-drafted insights, human-approved decisions, and an append-only audit trail.

The first scenario pack models a transportation-consulting world: proposal resources, market opportunity signals, and customer feedback themes. All clients, projects, and people in the data are fictional.

![Governance loop](docs/governance-loop.png)

## The loop

```
messy signals
  → searchable resource library
  → AI-drafted insights (deterministic, whitelisted commands only)
  → command preview (human review required)
  → approve / reject
  → append-only audit trail
```

AI helps structure and draft. It never executes anything on its own: every AI-proposed command is previewed, approved or rejected by a human, and logged.

## Demo script

Open **AI Governance** and use the three built-in prompt suggestions:

1. **"Summarize recurring customer feedback themes for the proposal team."**
   → generates a `summarizeFeedback` command → preview dialog → approve → a draft summary enters the Review Queue (still requires its own approval before it counts as reusable material).
2. **"Show high-confidence market opportunity signals."**
   → generates a `filterGrid` command → approve → the Signal Themes grid filters to High-confidence rows, with a visible "AI filter active" chip.
3. **"Draft a short insight memo for active transportation proposal planning."**
   → generates a `draftInsightMemo` command scoped to the Active Transportation segment → approve → the memo enters the Review Queue.

Then try a forbidden prompt, e.g. **"Delete all old feedback notes"** — it is auto-rejected with the rule name (`deleteData`) and a visible reason, and the attempt is logged.

## Why a deterministic parser (and not an LLM) in v1

I intentionally started with a deterministic parser to validate the command schema, review checkpoints, and audit trail first. The governance layer is the hard part worth testing; once the workflow boundary is stable, a real LLM can swap in behind the same validator without changing the review or audit semantics.

This also makes the demo reproducible: the same prompt always yields the same command, the same preview, and the same audit entries.

## Safety model

- **Whitelist**: the assistant can only propose seven commands — `filterGrid`, `draftInsightMemo`, `summarizeFeedback`, `compareMarketSegments`, `markNeedsReview`, `approveDraft`, `rejectDraft`.
- **Forbidden rules run first (deny-before-allow)**: prompts matching destructive or exfiltrating intent (`deleteData`, `sendExternally`, `writeToSourceSystem`, `rawSql`, `getApiKey`, `executeCode`) are rejected whole, with the rule name shown. A prompt that mixes a legitimate request with a forbidden action is rejected, not partially executed.
- **Approval gate**: an allowed command opens a preview dialog. Nothing executes until a human approves.
- **Append-only audit**: every AI-initiated action writes audit rows (generation and decision). Entries are never mutated. Manual grid filtering/sorting is deliberately *not* audited — the trail tracks AI activity, not user browsing.
- **Over-blocking is the chosen failure mode**: broad forbidden patterns (e.g. `\bsend\b`) can reject legitimate phrasings. That trade-off is intentional for a governance prototype.

## Scenario-neutral engine

All domain data lives in a `ScenarioPack` (segments, resources, themes, demo prompts). The engine never reads the scenario id. Adding a second scenario (e.g. a sales-pipeline world) is one new data file plus one DI registration line in `Program.cs`.

## Run locally

Requires the .NET 10 SDK and a **Telerik UI for Blazor license** (this repo contains no license keys; a free 30-day trial is available from Telerik).

```powershell
dotnet run --project src/SignalIntelligenceWorkspace --launch-profile http
```

Run the parser tests:

```powershell
dotnet test
```

## What this is — and is not

This is a small prototype built to **test an AI-assisted proposal / market-intelligence workflow**: how AI output should be bounded, reviewed, and recorded before it becomes reusable team material.

It is **not** a production AI tool, not an internal system of any company, and not a claim of AI product engineering or deployed tooling ownership. The scenario data is fictional.

## Roadmap

- Real LLM behind the same command validator
- Second scenario pack (sales pipeline / revenue operations world)
- Persistence across sessions
- Multi-role review simulation (coordinator / analyst / reviewer)

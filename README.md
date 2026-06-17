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

## Localization

The interface ships in English (default) and Traditional Chinese, switchable from the EN / 繁中
toggle in the top bar. UI chrome — navigation, headings, buttons, column titles, review-state
labels — is localized via standard .NET resource files (`Localization/Ui.resx`,
`Ui.zh-Hant.resx`). The switch writes a culture cookie and reloads, because the interactive-server
render mode fixes the culture when the circuit is created.

Two things stay in English by design: the scenario's mock data (the transportation-consulting
world is an English-language market) and the audit log's decision text. Audit entries record the
language in force when the action happened and are never rewritten — keeping them immutable is the
point of an audit trail, so switching the UI language does not retranslate past records.

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

### Optional AI-backed cockpit parsing

The `/cockpit` Grid command box works without an AI key by falling back to the
local deterministic parser. To try LLM-backed semantic parsing, configure a
server-side provider and key.

Gemini:

```powershell
dotnet user-secrets set "Ai:Provider" "Gemini" --project src/SignalIntelligenceWorkspace
dotnet user-secrets set "Gemini:ApiKey" "<your-gemini-api-key>" --project src/SignalIntelligenceWorkspace
dotnet user-secrets set "Gemini:Model" "gemini-2.5-flash" --project src/SignalIntelligenceWorkspace
```

OpenAI:

```powershell
dotnet user-secrets set "Ai:Provider" "OpenAI" --project src/SignalIntelligenceWorkspace
dotnet user-secrets set "OpenAI:ApiKey" "<your-api-key>" --project src/SignalIntelligenceWorkspace
dotnet user-secrets set "OpenAI:Model" "gpt-5.5" --project src/SignalIntelligenceWorkspace
```

You can also use environment variables: `AI_PROVIDER`, `GEMINI_API_KEY`,
`GEMINI_MODEL`, `OPENAI_API_KEY`, and `OPENAI_MODEL`. If no provider is set, the
app picks Gemini when a Gemini key exists, then OpenAI when an OpenAI key exists.
The app sends only the user's Grid prompt and the allowed command schema to the
configured provider, then validates the structured response before changing the
local Grid filter state.

## What this is — and is not

This is a small prototype built to **test an AI-assisted proposal / market-intelligence workflow**: how AI output should be bounded, reviewed, and recorded before it becomes reusable team material.

It is **not** a production AI tool, not an internal system of any company, and not a claim of AI product engineering or deployed tooling ownership. The scenario data is fictional.

## Roadmap

- More Grid command types behind the same bounded LLM response schema
- Second scenario pack (sales pipeline / revenue operations world)
- Persistence across sessions
- Multi-role review simulation (coordinator / analyst / reviewer)

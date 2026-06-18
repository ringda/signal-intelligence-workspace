# Signal Intelligence Workspace

A small **signal-to-action cockpit** that brings the live Application Tracker
and the HubSpot CRM workflow into one Blazor/Telerik workspace.

The shared pattern is:

```text
messy signals
  -> structured context
  -> human-reviewed next action
  -> governed writeback boundary
```

The workspace has three surfaces:

- **Application Tracker (`/cockpit`)**: reads the job-search system of record
  directly from `core.jobs`, `core.applications`, and `core.descriptions`, then
  shows the JD/application pipeline, evidence, readiness cards, and next-step
  reasoning.
- **HubSpot CRM (`/hubspot`)**: reads live HubSpot contacts, companies, and
  deals with a scoped private app token, then turns CRM hygiene into GTM handoff
  readiness cards.
- **AI Review (`/governance`)**: keeps the original fictional governance lab
  where AI-proposed actions are previewed, approved/rejected, and logged.

![Governance loop](docs/governance-loop.png)

## The Loop

```
messy signals
  → searchable resource library
  → AI-drafted insights (deterministic, whitelisted commands only)
  → command preview (human review required)
  → approve / reject
  → append-only audit trail
```

AI helps structure and draft. It never executes anything on its own: every AI-proposed command is previewed, approved or rejected by a human, and logged.

## Demo Script

Open `http://localhost:8240/cockpit` for the Application Tracker:

1. Confirm the page is reading from the direct Postgres source card.
2. Review today's application steps, weekly application pace, and readiness
   cards.
3. Use the semantic grid input, e.g. **"Show strong fits"** or
   **"Sales-related roles"**.
4. Ask the Application Tracker Assistant to prioritize jobs, explain risk, or
   draft next actions from the loaded snapshot.
5. Select a row and inspect the role summary, hiring focus, fit notes, and next
   step.

Open `http://localhost:8240/hubspot` for the HubSpot CRM surface:

1. Confirm contacts, companies, and deals load from the connected HubSpot
   portal.
2. Inspect the CRM Handoff Readiness cards.
3. Ask the HubSpot CRM Assistant to audit hygiene, rank cleanup work, or draft a
   next-touch memo from the loaded snapshot.
4. Open a record in HubSpot from the row or contact card.

The HubSpot page is read-only in this repo. The assistant can draft proposed
notes/tasks/field changes, but it does not execute CRM writes.

For the original AI governance lab, open **AI Review** and use the three built-in
prompt suggestions:

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

For the HubSpot page, set a private app token in the environment that launches
the app:

```powershell
setx PRIVATE_APP_ACCESS_TOKEN "pat-your-token-here"
```

Optional config keys:

```text
HubSpot:PortalId
HubSpot:UiDomain
HubSpot:PrivateAppAccessToken
```

```powershell
dotnet run --project src/SignalIntelligenceWorkspace --launch-profile http
```

Run the parser tests:

```powershell
dotnet test
```

### Optional AI-backed cockpit parsing and HubSpot assistant

The `/cockpit` Grid command box works without an AI key by falling back to the
local deterministic parser. The `/hubspot` CRM Assistant needs a Gemini key
because it generates narrative CRM hygiene analysis and memo drafts from the
loaded snapshot. To try the AI-backed flows, configure a server-side provider
and key.

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
local Grid filter state. The HubSpot assistant sends the currently loaded
HubSpot snapshot context to Gemini and returns text only; it does not call
HubSpot write APIs.

## What This Is — And Is Not

This is a portfolio prototype for **signal intelligence workflows**: job-market
signals and CRM signals become structured context, inspected next actions, and a
clear boundary before writeback.

It is not a production AI platform, not an official HubSpot project, not a
production HubSpot administration system, and not a claim of ownership over any
company's internal CRM/GTM system. The governance scenario data is fictional;
the Application Tracker and HubSpot pages can read real configured systems.

## Roadmap

- Add a dedicated HubSpot detail page for one contact/deal.
- Add confirmation-gated CRM write proposals after the read-only surface is
  stable.
- Add a shared "next actions" view across Application Tracker and HubSpot CRM.
- Keep the governance lab as the review/audit pattern for future write paths.

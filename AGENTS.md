# Signal Intelligence Workspace - Agent Rules

> `AGENTS.md` is the repo-level maintenance contract. Keep it for stable,
> cross-task rules that should apply every time. Put one-off plans, field specs,
> and implementation notes in more specific docs or code comments instead.

## Core Mission

This repo is a small Blazor/Telerik cockpit and governance prototype for a
signal-intelligence workflow: messy signals become searchable resources,
reviewable AI-drafted insights, human-approved next actions, and auditable
writeback boundaries.

The workspace now carries two inspectable signal surfaces in one app:

- Application Tracker: reads the job-search system of record directly from
  `core.jobs`, `core.applications`, and `core.descriptions` with read-only
  SQL.
- HubSpot CRM: reads live HubSpot CRM records through a scoped private app token
  and translates CRM hygiene into GTM handoff readiness.

The project is interview leverage first. Prefer the smallest credible demo that
can be inspected, explained, and reused across future scenario packs. Do not
turn the cockpit into a broad platform unless the user explicitly changes the
goal.

## Source Map

Start from the nearest durable source before acting.

- `AGENTS.md`: repo-level rules for maintenance, boundaries, verification, and
  handoff.
- `README.md`: public project story, demo script, safety model, run commands,
  and claim boundary.
- `src/SignalIntelligenceWorkspace/Components/Pages/Cockpit.razor`: `/cockpit`
  page structure and Telerik component usage.
- `src/SignalIntelligenceWorkspace/Components/Pages/HubSpot.razor`: `/hubspot`
  read-only HubSpot CRM cockpit.
- `src/SignalIntelligenceWorkspace/wwwroot/app.css`: cockpit styling and layout
  surface.
- `src/SignalIntelligenceWorkspace/Localization/Ui.resx` and
  `src/SignalIntelligenceWorkspace/Localization/Ui.zh-Hant.resx`: user-visible
  labels and localized cockpit text.
- `src/SignalIntelligenceWorkspace/Services/Cockpit/CockpitDataService.cs`:
  read-only direct Postgres access for the application cockpit. Treat this as
  data-boundary code, not UI surface.
- `src/SignalIntelligenceWorkspace/Models/Cockpit/`: cockpit DTOs that mirror
  the application tracker snapshot consumed by `CockpitDataService`.
- `src/SignalIntelligenceWorkspace/Services/HubSpot/HubSpotCrmService.cs`:
  read-only HubSpot API access and local CRM readiness logic ported from the
  HubSpot demo.
- `src/SignalIntelligenceWorkspace/Models/HubSpot/`: DTOs for the HubSpot CRM
  cockpit.
- `.mcp.json`: declares the Telerik Blazor MCP server. Its presence does not
  prove the current client has exposed the MCP tools; check the active tool list
  before relying on them.

## Working Order

1. Inspect the current worktree and relevant files first. Do not rely on memory
   or previous thread state when the repo can answer the question.
2. Identify the layer being changed: UI, localization, data access, model,
   scenario engine, tests, or docs.
3. Keep changes in the smallest layer that solves the request. Do not refactor
   adjacent code just because it is nearby.
4. Define the verification evidence before editing. For cockpit work, evidence
   usually means build, tests, and browser verification of `/cockpit`.
5. Report completion with concrete evidence, warnings, and any remaining risk.

## Cockpit Boundaries

For UI-only cockpit work, the normal editable surface is:

- `src/SignalIntelligenceWorkspace/Components/Pages/Cockpit.razor`
- `src/SignalIntelligenceWorkspace/wwwroot/app.css`
- `src/SignalIntelligenceWorkspace/Localization/Ui*.resx` when labels or copy
  change

Do not modify these for UI-only work unless the user explicitly asks or the
current evidence proves they are necessary:

- `src/SignalIntelligenceWorkspace/Services/Cockpit/CockpitDataService.cs`
- `src/SignalIntelligenceWorkspace/Models/Cockpit/`
- direct SQL backing `CockpitDataService`
- user-secrets or local secret configuration
- `D:\claude-workspace\personal\xin-job-hunting` DB source

The application cockpit may read from the job-search system of record through
`CockpitDataService`. It should stay read-only unless the user explicitly opens
a write path. UI maintenance should not silently widen that data-access
boundary.

The HubSpot cockpit is read-only unless the user explicitly opens a write path.
Do not add create/update/delete behavior, MCP write tools, or assistant-driven
CRM writes without preserving the explicit confirmation gate and documenting the
permission boundary.

## Telerik UI Rules

Telerik UI changes need Telerik-grounded evidence. Do not implement Telerik
components from generic Blazor intuition alone.

Before changing Telerik components, layout primitives, chart/grid behavior, or
component parameters:

1. Check whether the current client exposes Telerik MCP tools or assistants such
   as `Telerik.Blazor.MCP`, `#telerik_ui_generator`, or
   `component-layout-styling`.
2. If they are available, use them to validate component APIs, examples, and
   layout guidance.
3. If they are not available, say so in the work log or final response, then use
   Telerik official docs or official sample source before implementing.
4. Keep `grid-get-data`-style capabilities off by default for sensitive
   CRM/job-search data unless the user explicitly authorizes that data access.

Treat Telerik Blazor UI components as production-usable UI components. Treat
Telerik WebMCP and AI-adjacent capabilities as preview/experimental unless
current official docs prove otherwise.

## Verification Gate

After any repo change, run:

```powershell
dotnet build
dotnet test
```

For cockpit or layout-visible work, also run the app and verify `/cockpit` in a
browser:

```powershell
dotnet run --project src/SignalIntelligenceWorkspace --launch-profile http
```

Verify `http://localhost:8240/cockpit` shows the cockpit page, has no cockpit
alert, renders the Telerik chart and grid, and has no fresh console error after
the page load. If an existing dev server locks `bin/` or `obj/`, identify and
stop the repo's own `SignalIntelligenceWorkspace` process before rebuilding,
then restart it for browser verification.

Known acceptable warning: Telerik licensing may emit warnings such as `TKL002`,
`TKL101`, or `TKL004` when no local Telerik license is configured. Report the
warning, but do not treat it as a build failure if the command exits
successfully.

## Language and Encoding

- User-facing discussion can be in Traditional Chinese.
- Repo rules and durable docs may use Traditional Chinese or English; keep the
  file internally consistent.
- On Windows PowerShell, read Markdown and resource files with an explicit UTF-8
  reader when content matters:

```powershell
Get-Content -Raw -Encoding UTF8 <path>
```

If terminal output appears garbled, stop drawing conclusions from that output
and reread with an explicit UTF-8 reader.

## Git and Handoff

- Inspect `git status --short --branch` before editing and before finishing.
- Stage narrowly. Do not use `git add .` or `git add -A` when unrelated dirty
  files exist.
- Commit or push only when the user asks, or when a repo-specific workflow
  explicitly requires it.
- Final handoff should include changed files, verification commands, browser
  result, and any warnings or blockers.

## Rule Updates

When a maintenance lesson is worth keeping, prefer one precise rule in
`AGENTS.md` over scattered reminders in chat. Do not add guardrails for behavior
that the agent would already do correctly. Add rules when they prevent a real
failure mode, protect a source-of-truth boundary, or make verification
repeatable.

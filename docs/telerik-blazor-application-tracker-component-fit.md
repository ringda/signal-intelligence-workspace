# Telerik Blazor Component Fit for Application Tracker

Status: component-fit review  
Scope: Application Tracker cockpit, future application pages, outreach pages,
follow-up calendar, weekly review, and governed AI workflow  
Reviewed against: Telerik UI for Blazor 14.0.0 in this repo  
Last reviewed: 2026-06-16

## Source And Boundary

This review maps the official Telerik UI for Blazor component catalog to the
Application Tracker product spec in this repo. It is a product-design decision
aid, not an implementation plan.

Official source context:

- Telerik UI for Blazor product page lists 120+ components across AI Interface,
  Navigation, Data Management, File Upload & Management, Diagrams and Maps,
  Labels, Icons, Data Visualization, Editors, Scheduling, Layout, Gauges,
  Interactivity & UX, Reporting, and Document Processing.
- Telerik UI for Blazor 14.0.0 release notes list WebMCP support and the new
  TaskBoard component.
- Current Telerik official AI surface includes AI Prompt, Inline AI Prompt,
  PromptBox, SmartPasteButton, AI tools / MCP Servers, and Grid Smart AI
  Features. Treat the AI UI as a governed workflow surface, not as autonomous
  application-decision logic.

Repo context:

- Package: `Telerik.UI.for.Blazor` version `14.0.0`.
- Existing tracker surface: `src/SignalIntelligenceWorkspace/Components/Pages/Cockpit.razor`.
- Existing Telerik usage: Drawer, Button, Grid, Chart, Dialog, Card.
- Current MCP state: no Telerik MCP tool is exposed to this client, so future
  component API work should use official Telerik docs or sample source.

## Fit Scale

- `Core`: should shape the product.
- `Strong`: very useful once the data surface exists.
- `Situational`: useful in a specific page or workflow.
- `Low`: possible, but not worth prioritizing.
- `Avoid`: likely to add weight or drift from the product.
- `Infra`: developer/runtime tooling, not user-facing tracker UI.

## Most Interesting Candidates

These are the unusually strong fits, not just generic dashboard parts.

1. `TaskBoard`: Could become an application-flow board with columns like
   `To apply`, `Tailoring`, `Applied`, `Outreach`, `Follow-up`, `Interview`.
   Use only when status write-back is allowed.
2. `Splitter`: A better cockpit workbench shape: application grid on the left,
   evidence / next action / outreach detail on the right.
3. `Stepper`: Best visual model for one application's completeness:
   shortlist -> tailor -> apply -> outreach -> follow-up -> review.
4. `Scheduler`: Best for follow-up discipline, especially Agenda view for due
   and overdue actions.
5. `PDF Viewer`: Surprisingly strong for application detail if the tracker
   shows exported resume, cover letter, JD PDF, or evidence artifacts.
6. `Sankey Chart`: Very strong for weekly review: source -> shortlisted ->
   applied -> replies -> interviews.
7. `Heatmap`: Strong for behavior review: days with review, tailoring,
   outreach, follow-up, replies.
8. `SmartPasteButton`: Potentially special if used to parse pasted job details
   into a draft application record, but it must stay behind human review.
9. `PromptBox` / `AI Prompt`: Useful only if wired into the existing governance
   idea: draft suggestions, preview, approve, append audit.
10. `Grid Smart AI Features`: Potentially strong for natural-language filtering
    such as "show applications with follow-up due this week", but keep it away
    from sensitive raw data until the backend endpoint and prompt boundary are
    approved.
11. `Spreadsheet`: Good as an import/export bridge from old job trackers, not
    as the main product UI.

## Application Tracker Vocabulary Map

Use these terms in the UI and component planning so the tracker reads as a
Marketing / GTM signal system, not just a job-search spreadsheet.

| System term | HM-facing meaning | Telerik components to use |
|---|---|---|
| Signal funnel | Large job-market inputs narrowing into a qualified pipeline | `Stepper`, `Sankey Chart`, `Column Chart`, `ProgressBar` |
| Today's next actions | What needs human attention now | `Data Grid`, `Badge`, `Chip`, `ToolBar`, `Popover` |
| Application pipeline | Stage of each role from review to interview signal | `Data Grid`, `TaskBoard`, `Stepper`, `ChipList` |
| Decision evidence | Why this role deserves effort and what risk remains | `Splitter`, `TabStrip`, `Card`, `PanelBar`, `Tooltip` |
| Tailoring state | Whether resume / CL / memo work is not started, in progress, ready, or sent | `Stepper`, `ProgressBar`, `DropDownList`, `TextArea` |
| Outreach judgment | Which stakeholder to contact, why, and when to follow up | `Scheduler`, `Calendar`, `Data Grid`, `Popover`, `Avatar` |
| Interview / reply signal | Inputs that start converting into outcomes | `Badge`, `Line Chart`, `Column Chart`, `Donut Chart` |
| Learning write-back | What pattern should change the next run | `Sankey Chart`, `Heatmap`, `Bar Chart`, `TextArea`, `Dialog` |
| Governed AI assist | AI drafts or structures, human previews and approves | `SmartPasteButton`, `PromptBox`, `AI Prompt`, `Inline AI Prompt`, `Dialog` |
| Legacy tracker bridge | Spreadsheet-style import/export, not the primary UI | `Spreadsheet`, `SpreadProcessing`, `SpreadStreamProcessing` |

## Recommended Adoption Levels

### Build The First Cockpit With These

- `Data Grid`: the central application table. It should carry decision words,
  not just status words: worth pursuing, tailoring state, outreach state, next
  action, and signal strength.
- `Splitter`: the clearest structure for grid on the left and decision evidence
  on the right.
- `Stepper`: the cleanest visual language for signal -> review -> tailoring ->
  applied -> follow-up -> interview signal.
- `Chip` / `Badge`: compact labels for due, blocked, applied, high priority,
  reply, and interview signal.
- `Chart`, especially `Line Chart`, `Column Chart`, and `Bar Chart`: enough for
  pace, weekly volume, and source quality without making the UI decorative.
- `Tooltip`, `Popover`, `Dialog`, `Skeleton`, `LoaderContainer`: small UX
  pieces that make the cockpit feel real and inspectable.

### Add Next When The Data Exists

- `TaskBoard`: use when applications can safely move between workflow stages.
  It is special, but only after status write-back is designed.
- `Scheduler` / `Calendar`: use for follow-up and interview discipline.
- `TabStrip`: use for application detail: Evidence, Documents, Outreach,
  History.
- `PDF Viewer`: use when resume, cover letter, JD, or evidence packets are
  visible inside an application detail.
- `Sankey Chart` and `Heatmap`: use for weekly review and learning loop, not
  the first daily cockpit if data is still thin.
- `PivotGrid`: reserve for later analysis by source, lane, stage, outcome, and
  time period.

### Use AI Components Carefully

- `SmartPasteButton`: best AI fit for this product. It can turn a pasted JD,
  LinkedIn snippet, recruiter message, or reply into draft structured fields.
  Never auto-commit the result.
- `PromptBox`: best for a governed assistant panel where the user asks for a JD
  summary, outreach draft, or next-action suggestion.
- `AI Prompt`: useful for a more explicit AI workspace, but it should not take
  over the cockpit first screen.
- `Inline AI Prompt`: useful inside a note, fit reasoning, or reply summary
  field, but risky if it makes the page feel like it is auto-writing final
  claims.
- `Grid Smart AI Features`: useful later for natural-language filtering and
  sorting. Do not expose raw job-search / CRM-like data to an AI backend until
  the endpoint, prompt boundary, logging, and human-review policy are explicit.

## Full Component Review

### AI Interface

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| AI Prompt | Situational | Good for governed drafting: summarize JD, propose next action, generate outreach draft. Not a first-screen control. |
| Inline AI Prompt | Situational | Useful inside notes or fit reasoning fields, but risky if it makes final text feel auto-generated. |
| PromptBox | Strong | Best AI input primitive if the app adds a controlled assistant panel with preview and approval. |
| SmartPasteButton | Strong | Very interesting for pasting a JD or LinkedIn job snippet into structured draft fields, with human confirmation. |

AI-adjacent feature to track: `Grid Smart AI Features` are not a separate
catalog component, but they matter for this app because the grid is the central
work surface. The right future use is natural-language grid operations such as
"show follow-ups due this week" or "filter roles with strong signal but no
outreach." The wrong use is letting AI inspect or rewrite sensitive application
records without an approved backend endpoint and audit boundary.

### Navigation

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| AppBar | Strong | Good top shell for date, culture, sync status, and compact global actions. |
| Breadcrumb | Low | Useful only after adding deep application detail pages. |
| Button Group | Situational | Good for compact filters like Today / Week / Month or fit-level toggles. |
| Buttons | Core | Required for actions, but keep icon-first and sparse. |
| Chip | Core | Excellent for priority, fit, source, status, stakeholder type, and due-state labels. |
| ChipList | Strong | Good for multi-select filters: source, status, role lane, stakeholder type. |
| Context Menu | Situational | Useful for row actions such as open folder, mark applied, add follow-up. Avoid hiding primary actions. |
| Drawer | Core | Already used; good for navigation between cockpit, applications, outreach, weekly review. |
| DropDownButton | Situational | Good for secondary row actions, export, or grouped create actions. |
| Floating Action Button | Low | Could add new application quickly, but may feel mobile-app-ish in this dense cockpit. |
| Menu | Low | Drawer and toolbar are enough unless the app grows into multiple modules. |
| Segmented Control | Core | Excellent for mode/view filters: Cockpit / Applications / Outreach / Follow-ups / Review. |
| Speech-to-Text Button | Low | Interesting for quick notes after calls, but too peripheral for the current prototype. |
| Split Button | Situational | Good for primary action plus variants, such as Add application / paste JD / import row. |
| Stepper | Core | The strongest process component for per-application workflow progress. |
| Switch | Situational | Useful for binary filters or settings; not central. |
| TabStrip | Strong | Best way to divide application detail into Evidence, Documents, Outreach, History. |
| Toggle Button | Situational | Good for starred/high-priority or compact boolean filter states. |
| ToolBar | Strong | Good for grid filters, saved views, search, export, and bulk action entry points. |
| TreeView | Low | Only useful if source folders or application artifacts become a navigable hierarchy. |

### Data Management

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| Data Grid | Core | Main application tracker and outreach tracker should be Grid-first. |
| Filter | Strong | Useful for an advanced filter builder once applications grow beyond simple grid menus. |
| ListView | Situational | Good for mobile/narrow cards or compact next-action feeds. Grid remains primary. |
| Pager | Situational | Needed if grids become large or server-paged. |
| PivotGrid | Situational | Good for weekly review by source, status, role type, or conversion stage. Too heavy for first page. |
| Spreadsheet | Situational | Useful for importing/exporting legacy tracker sheets, not as the app's main interface. |
| TaskBoard | Core | Very special fit for workflow-stage visualization if drag/drop status updates are allowed. |
| TreeList | Situational | Useful for Company -> Role -> Contacts or Application -> Tasks hierarchy. Avoid until hierarchy is real. |

### File Upload & Management

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| DropZone | Situational | Good for dropping JD PDFs, resumes, CLs, or screenshots into an application record. |
| File Manager | Low | Too much file-system surface for this prototype unless artifact management becomes the product. |
| FileSelect | Situational | Good for attaching a resume/CL/JD file to a record. |
| PDF Viewer | Strong | Strong fit for inspecting exported resume, cover letter, JD, or evidence packet in detail view. |
| Upload | Situational | Useful if documents are persisted through app storage. Not needed for read-only cockpit. |

### Diagrams And Maps

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| Diagram | Situational | Could show governed workflow or application lifecycle, but likely over-designed for daily use. |
| Map | Low | Only useful if location strategy becomes a real decision surface. |

### Labels

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| Badge | Core | Excellent for counts: overdue follow-ups, unread replies, missing outreach, high priority. |
| FloatingLabel | Situational | Fine for compact forms, but normal labels may be clearer in operational tools. |

### Icons

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| FontIcon | Situational | Fine if already used, but prefer one icon system consistently. |
| SVGIcon | Core | Good for toolbar and button icons; current repo already uses `SvgIcon.Menu`. |

### Data Visualization

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| Area Chart | Situational | Good for cumulative weekly activity, but line/column may be clearer. |
| Bar Chart | Strong | Good for source quality, role types, outreach response counts. |
| Barcode | Avoid | No real tracker use. |
| Bubble Chart | Low | Could map fit score vs posting age vs priority, but likely too analytical for MVP. |
| Candlestick Chart | Avoid | Finance-specific; no fit. |
| Chart | Core | Existing generic chart wrapper; keep for pace and review visuals. |
| Column Chart | Strong | Good for daily/weekly counts and conversion by category. |
| Donut Chart | Situational | Good for status mix; avoid if it becomes decorative. |
| Heatmap | Strong | Very good for habit/coverage review across days and stages. |
| Line Chart | Core | Already appropriate for application pace vs target over time. |
| OHLC Chart | Avoid | Finance-specific; no fit. |
| Pie Chart | Low | Donut or bar is usually better for status mix. |
| QR Code | Low | Possible for sharing a public portfolio/contact link, not tracker core. |
| Radar Area Chart | Low | Could visualize readiness dimensions, but may feel gimmicky. |
| Radar Column Chart | Low | Same as radar area; not a product priority. |
| Radar Line Chart | Low | Same as radar area; use only for interview-readiness self-assessment. |
| Range Area Chart | Low | Possible for target bands, but not needed soon. |
| Range Bar Chart | Situational | Could show application stage duration windows. |
| Range Column Chart | Situational | Could show weekly target bands. |
| Sankey Chart | Strong | Excellent for weekly funnel: source -> shortlist -> applied -> reply -> interview. |
| Scatter Chart | Situational | Good for analyzing fit score vs posting age or effort vs outcome. |
| Scatter Line Chart | Low | Less obvious than scatter or line for this app. |
| Stock Chart | Avoid | Finance-specific; no tracker fit. |
| Trendline Chart | Situational | Good if weekly review predicts pace or conversion trend. |
| Waterfall Chart | Situational | Could explain weekly pipeline change, but likely more executive-report than daily tool. |

### Editors

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| AutoComplete | Strong | Good for company, role title, source, stakeholder names, skills. |
| CheckBox | Core | Required for flags like CL needed, remote ok, follow-up done. |
| ColorGradient | Avoid | No meaningful tracker use. |
| ColorPalette | Low | Useful only in settings for status colors; not a workflow feature. |
| ColorPicker | Low | Same as ColorPalette. |
| ComboBox | Strong | Good for searchable controlled fields such as company or role lane. |
| DateInput | Situational | Good for exact dates in dense forms. |
| DatePicker | Core | Essential for follow-up date, submitted date, interview date. |
| DateRange Picker | Strong | Good for review periods and dashboard filters. |
| DateTimePicker | Situational | Good for interviews and calls. |
| DropDownList | Core | Status, priority, source, stakeholder type, application source. |
| DropDownTree | Low | Only if taxonomy gets nested, such as industry -> function -> role. |
| FlatColorPicker | Low | Same as color settings; not core. |
| ListBox | Situational | Good for assigning selected outreach targets or moving contacts between lists. |
| MaskedTextBox | Low | Phone numbers or structured IDs only; peripheral. |
| MultiColumn ComboBox | Strong | Very useful for selecting company/contact with multiple visible fields. |
| MultiSelect | Strong | Good for tags, skills, role lanes, sources, stakeholder coverage. |
| Numeric TextBox | Situational | Fit score, priority score, target counts. Use sparingly. |
| RadioGroup | Strong | Good for mutually exclusive workflow choices: apply now vs wait referral. |
| Rating | Low | Could show fit confidence, but a controlled score is clearer. |
| Rich Text Editor | Low | Avoid for tracker notes unless long memo drafting becomes central. |
| Signature | Avoid | No fit. |
| TextArea | Core | Fit notes, learning notes, reply summary, next-action context. |
| TextBox | Core | Basic fields and search-like inputs. |
| TimePicker | Situational | Interviews, calls, or time-block planning. |

### Scheduling

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| Calendar | Strong | Good compact month view for follow-ups and interview dates. |
| Gantt | Low | Could show application durations, but likely overstates project-management complexity. |
| Scheduler | Core | Strong for follow-up due dates, interview calendar, weekly review blocks, and agenda view. |

### Layout

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| Animation Container | Low | Use lightly for disclosure; avoid decorative motion. |
| Avatar | Situational | Useful for contacts/stakeholders if outreach pages show people. |
| Card | Situational | Good for repeated detail cards; avoid nested dashboard-card overload. |
| Carousel | Avoid | Not appropriate for an operational tracker. |
| DockManager | Low | Interesting for customizable analyst workspace, but too heavy for the prototype. |
| Form | Core | Main editor for applications, outreach records, follow-up records. |
| GridLayout | Strong | Good for stable dashboard layout with controlled responsive regions. |
| MediaQuery | Strong | Useful for responsive cockpit behavior without making separate pages. |
| PanelBar | Situational | Good for collapsible evidence groups or settings, not main navigation. |
| Splitter | Core | Excellent for grid/detail cockpit and resizable evidence workspace. |
| StackLayout | Strong | Good for disciplined vertical/horizontal layout primitives. |
| TileLayout | Situational | Good only if user-customizable dashboard tiles become a requirement. |
| Tooltip | Core | Good for icon buttons, status chips, and shorthand metrics. |
| Window | Situational | Good for transient detail or document preview, but prefer inline detail where possible. |
| Wizard | Situational | Good for guided creation/import of an application record. Not for daily dashboard. |

### Gauges

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| Arc Gauge | Low | Could show weekly progress, but bar/progress components are clearer. |
| Circular Gauge | Low | Same; visually heavy for operational dashboard. |
| Linear Gauge | Situational | Could show weekly target progress if styled quietly. |
| Radial Gauge | Low | Too dashboard-y for this product's restrained workflow. |

### Interactivity And UX

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| Chat | Situational | Could host a governed assistant, but only with preview/approval/audit. Not first. |
| ChunkProgressBar | Situational | Good for parsing/import progress when ingesting many jobs or documents. |
| Dialog | Strong | Already used; good for confirmation and governed action preview. |
| Loader | Core | Basic loading state. |
| Loader Container | Strong | Good for grid/detail loading without page jumps. |
| Notification | Situational | Good for save/sync/follow-up reminders, but avoid noisy alerts. |
| Popover | Strong | Good for compact row previews, contact details, or next-action explanation. |
| Popup | Situational | Lower-level primitive; use higher-level Popover/Dialog where possible. |
| ProgressBar | Strong | Good for per-application completeness and weekly progress. |
| RangeSlider | Situational | Good for filtering fit score or posting age ranges. |
| Skeleton | Strong | Good polished loading state for cockpit cards and grids. |
| Slider | Low | Numeric thresholds only; not core. |
| ValidationMessage | Core | Required for forms. |
| ValidationSummary | Situational | Useful in larger forms or wizard steps. |
| ValidationTooltip | Strong | Good for dense forms where inline errors would crowd the layout. |

### Productivity Tools

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| Visual Studio Code | Infra | Useful developer tooling, not product UI. |

### Reporting

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| Reporting Integration | Situational | Useful if weekly/monthly job-search reports become exportable artifacts. Not MVP. |

### Document Processing

| Component | Fit | Application Tracker judgment |
|---|---:|---|
| PDFProcessing | Situational | Useful to generate evidence packets or export reviewed application summaries. |
| SpreadProcessing | Situational | Useful for import/export of tracker spreadsheets. |
| SpreadStreamProcessing | Situational | Useful for large spreadsheet import/export if tracker data grows. |
| WordsProcessing | Situational | Useful for generating DOCX resume/CL review packets, but outside current cockpit. |
| ZipLibrary | Situational | Useful for bundling application artifacts for export. |

## Recommended Component Architecture

### MVP Cockpit

- `GridLayout` or existing CSS grid for stable sections.
- `Stepper` for today's application workflow.
- `Chart` with `Line Chart` and `Column Chart` for pace.
- `Data Grid` for recent applications.
- `Splitter` for grid plus selected-application detail.
- `Chip`, `Badge`, `Tooltip`, `ProgressBar`, `Skeleton`, and `LoaderContainer`
  as supporting primitives.

### Application Detail

- `TabStrip` with Evidence, Documents, Outreach, History.
- `PDF Viewer` for resume/JD/CL artifacts.
- `Form`, `DropDownList`, `DatePicker`, `TextArea`, `MultiSelect`,
  `ValidationTooltip`.
- `Stepper` for workflow completeness.

### Outreach Page

- `Data Grid` or `TreeList` if the hierarchy is real.
- `ChipList` for stakeholder filters.
- `Popover` for quick contact/reply preview.
- `Scheduler` or `Calendar` for follow-up dates.

### Weekly Review

- `Sankey Chart` for source-to-outcome flow.
- `Heatmap` for weekly activity coverage.
- `Bar Chart` / `Column Chart` for source and role-pattern comparison.
- `PivotGrid` only if slicing by multiple dimensions becomes necessary.

### Governed AI Layer

- `SmartPasteButton` for job-snippet parsing into draft fields.
- `PromptBox` or `AI Prompt` for controlled assistant commands.
- `Dialog` for preview/approval.
- Existing audit-grid model for append-only trace.

## Components To Resist

- `Gantt`, unless applications truly need project-plan semantics.
- `Carousel`, because it weakens operational scanning.
- `Rich Text Editor`, unless long-form memo writing becomes the product.
- `Map`, unless location strategy becomes a real workflow.
- Finance-specific charts such as Candlestick, OHLC, and Stock Chart.
- Heavy customizable workspace components such as `DockManager` until the
  product has enough user roles to justify it.

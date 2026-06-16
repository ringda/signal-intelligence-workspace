# Application Tracker Product Spec

Status: derived product contract  
Scope: `SignalIntelligenceWorkspace` cockpit and future tracker pages  
Last reviewed: 2026-06-16

## Purpose

The Application Tracker turns a job-search process into an inspectable workflow.
Its job is not to record every job found online. Its job is to show which roles
are worth pursuing, what has already been done, what evidence supports the
decision, and what the next action should be.

This document is the bridge between the TechTalk Pillar 4 course material and
this Blazor/Telerik prototype. The source material stays in the job-search
reference repo; this repo keeps only the product rules needed to design,
maintain, and explain the dashboard.

## Source Map

Primary source files:

- `../xin-job-hunting/shared/references/external-systems/techtalk/course-notes/pillar-4-lesson-19-module-1-job-application-101-workbook.md`
- `../xin-job-hunting/shared/references/external-systems/techtalk/course-notes/pillar-4-lesson-20-module-2-faqs.md`
- `../xin-job-hunting/shared/references/external-systems/techtalk/course-notes/pillar-4-lesson-21-resources-guide.md`

Supporting source files:

- `../xin-job-hunting/shared/references/external-systems/techtalk/course-toc.md`
- `../xin-job-hunting/shared/references/external-systems/techtalk/community-premium-resources/ai-knowledge-index.md`
- `../xin-job-hunting/shared/references/external-systems/techtalk/community-premium-resources/_index.md`

Source-use boundary:

- Do not copy the full course notes, tracker templates, screenshots, or paid
  resource text into this repository.
- Use source material as private design context and paraphrase it into product
  rules.
- If this prototype is shown publicly, describe the tracker as an original
  workflow dashboard inspired by application operating-system principles, not
  as a republication of TechTalk course content.
- Runtime data truth remains the existing application database and Supabase
  views consumed by `CockpitDataService`; course tracker columns are design
  input, not runtime schema by default.

## Product Promise

The tracker should answer five questions quickly:

1. Which roles deserve attention now?
2. Which application step is each role currently in?
3. Has the resume or cover letter work been completed?
4. Has networking or referral outreach started?
5. What is the next concrete action and when should it happen?

If a UI element does not help answer one of these questions, it should earn its
place carefully.

## Workflow Model

The tracker should model this motion:

```text
shortlist -> role decision -> resume tailoring -> submit application
          -> outreach / referral -> follow-up -> result review
          -> weekly learning loop
```

This sequence is more important than the exact screen shape. The current
`/cockpit` page can remain the first dashboard, with deeper pages added only
when they reduce clutter or support a repeated workflow.

## Core Records

### Application Record

Minimum useful fields:

- `date_added`: when the role entered the shortlist
- `company`
- `role_title`
- `location_or_remote_policy`
- `job_link`
- `source`
- `posting_age`
- `priority`: high, medium, or low
- `fit_notes`: why the role is worth pursuing and what risk remains
- `work_authorization_note`
- `resume_status`: not started, tailoring, ready, exported
- `cover_letter_status`: not needed, required, optional, submitted
- `application_status`: to apply, applied, interview, rejected, closed
- `submitted_date`
- `application_source`: company site, LinkedIn, referral, Easy Apply, other
- `next_action`
- `follow_up_date`
- `result_learning`
- `application_folder`

### Outreach Record

Minimum useful fields:

- `application_id`
- `contact_name`
- `stakeholder_type`: recruiter, hiring manager, peer, alumni, referral path
- `contact_url`
- `message_status`: not sent, sent, replied, call booked, referred
- `sent_date`
- `follow_up_date`
- `reply_summary`
- `next_action`

Outreach should not live as a vague note hidden inside the application row once
the workflow needs repeated follow-up. A separate page or detail panel becomes
useful when contact coverage, reply state, and follow-up date need to be scanned
across multiple applications.

## Dashboard Shape

The first screen should stay work-focused and scannable:

- Today's application steps: role review, resume tailoring, application sent,
  networking started, follow-up due.
- Weekly pace: applications sent, target-role reviews, good-fit roles, resume
  customizations, outreach started.
- Application pace chart: applications and reviewed roles over time.
- Recent job reviews: company, role, next step, fit level, notes.
- Detail panel: role summary, employer needs, fit reasoning, risk notes, folder,
  and the next action.

The dashboard should prefer "what needs attention" over "what exists in the
database." Dense operational views are welcome; decorative landing-page patterns
are not.

## Future Pages

Add pages only when the workflow naturally outgrows the dashboard.

- Application detail: full job evidence, tailoring state, documents, decision
  history, and next action.
- Outreach tracker: contacts per application, stakeholder mix, message status,
  follow-up dates, replies, and referral path.
- Weekly review: source quality, role-type patterns, reply signals, interview
  conversion, stalled applications, and lessons to feed back into resume,
  LinkedIn, outreach, and positioning.
- Settings or source map: data freshness, connected views, source boundaries,
  and claim boundary for demos.

## Metrics

Useful metrics:

- roles reviewed today and this week
- good-fit roles found
- applications submitted today and this week
- resume customizations completed
- networking started
- outreach target coverage per high-priority application
- follow-ups due or overdue
- replies, calls, referrals, interviews, rejections
- conversion by source, role type, and message pattern

Avoid metrics that reward blind volume. A high application count with weak fit,
no tailoring, and no follow-up should not look like success.

## UX Rules

- Every application row should expose a next action or make its absence obvious.
- The tracker should not encourage adding every job found online; shortlist
  first, then track roles worth real effort.
- Fit, priority, and follow-up should be visible without opening multiple
  nested panels.
- Resume and outreach progress should be represented as workflow state, not
  hidden prose.
- Weekly review should surface patterns that can improve future applications.
- Do not add fields just because the source tracker has them. Add fields when
  they change a decision, reduce memory load, or make follow-up harder to miss.

## Current Implementation Fit

Existing cockpit pieces already align with this spec:

- `Cockpit.razor` provides the `/cockpit` dashboard.
- `CockpitDataService` reads the current `core.cockpit_*` views.
- The current dashboard already has application steps, pace metrics, a chart,
  recent jobs, and an evidence detail panel.

Known gaps before expanding the UI:

- The current visible grid does not yet expose `next_action` as the primary
  decision field.
- Resume, cover letter, outreach, and follow-up status may not be fully exposed
  by the existing `core.cockpit_*` views.
- Contact-level outreach likely needs its own data contract before building a
  serious page.
- Weekly learning metrics need a clear source-of-truth query before they become
  dashboard claims.

## Backlog

1. Make `next_action` and `follow_up_date` first-class cockpit concepts once
   the data view exposes them.
2. Add an outreach coverage indicator for high-priority roles.
3. Add resume and cover-letter state to the pipeline if the data source can
   distinguish those steps reliably.
4. Add a weekly review view that summarizes source quality, role patterns,
   reply signals, and conversion.
5. Keep the detail panel evidence-based: role summary, hiring focus, fit
   reasoning, risk notes, and application folder should remain inspectable.
6. Only add new database fields or Supabase views after the UI need is clear and
   the data boundary has been explicitly approved.

## Verification Expectation

For doc-only changes, run:

```powershell
dotnet build
dotnet test
```

For cockpit or layout-visible changes, also run the app and verify
`http://localhost:8240/cockpit` in a browser.

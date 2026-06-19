---
name: hubspot-gtm-workflow
description: >-
  Bridge natural-language GTM activity, HubSpot CRM, and lightweight launch
  tracking for SDR/BDR and retail GTM workflows. Use this skill in three
  situations: (1) INBOUND/WRITE — the user describes a
  prospect signal in plain language (a meeting note, a LinkedIn reply, an
  inbound form, "just talked to...") and wants it captured as a structured,
  CRM-ready record written to HubSpot; (2) OUTBOUND/READ — the user names a
  contact/company/deal already in HubSpot and wants their CRM context pulled
  and turned into a GTM action memo (qualification + next best action + draft
  outreach); (3) RETAIL LAUNCH READINESS — the user describes channel launch
  risk, SKU/product data, pricing, inventory, partner readiness, or a weekly
  business review and wants a channel-readiness memo. Trigger on phrases like
  "log this prospect", "add to CRM",
  "capture this signal", "what should I do next with <name>", "prep me for
  <contact>", "write a follow-up plan from the CRM", "audit channel readiness",
  "log this launch risk", or "draft a weekly GTM memo".
---

> Moved from the retired `hubspot-crm-demo/.agents/skills/hubspot-gtm-workflow`
> on 2026-06-19. Keep this as a reference for the original local HubSpot MCP
> write/read workflow, not as active Signal Intelligence Workspace behavior.
> Recheck current HubSpot remote MCP/API behavior before implementing writes.

# HubSpot GTM Workflow

This skill connects unstructured GTM judgment with structured CRM facts via the
official HubSpot MCP server (`@hubspot/mcp-server`). The CRM stores **customer
and partner facts**; a launch tracker stores **product facts**; this skill
preserves **GTM judgment** — how to qualify a signal, assess risk, choose an
owner, and decide what to do next.

It runs in three directions. Decide which the user wants from their phrasing,
then follow that flow.

---

## Direction A — INBOUND (natural-language signal → CRM record)

**Goal:** turn a free-text prospect signal into a clean, deduplicated HubSpot
record (and an optional activity log), preserving GTM qualification judgment.

### Steps

1. **Parse the signal.** Extract every fact present: person name, email,
   company, job title, phone, the source/channel, and any buying signal
   (pain, timeline, budget hint, competitor mention, trigger event).

2. **Apply GTM judgment (do not skip).** Before writing, assess:
   - **ICP fit** — does the company/role match the target profile? Note why.
   - **Signal strength** — explicit intent ("we're evaluating now") vs. soft
     ("looked at pricing"). Map to a lead status.
   - **Lifecycle stage** — lead / marketingqualifiedlead / salesqualifiedlead /
     opportunity, based on the signal.
   Record this judgment in the record's notes — it is the part a raw CRM import
   would lose.

3. **Deduplicate first.** Call `hubspot-search-objects` on `contacts` filtered
   by email (or name + company if no email) BEFORE creating. If a match exists,
   prefer `hubspot-batch-update-objects` over creating a duplicate.

4. **Map to properties** (see Property Reference below) and **confirm with the
   user** the record you're about to write — these are write operations.

5. **Write.** Use `hubspot-batch-create-objects` (new) or
   `hubspot-batch-update-objects` (existing) on the `contacts` object. If a
   company or deal is implied, create/associate it too
   (`hubspot-batch-create-associations`).

6. **Log the signal as an engagement.** Use `hubspot-create-engagement` to add
   a NOTE capturing the original signal text + your qualification judgment, so
   the reasoning is auditable in HubSpot.

7. **Confirm back** with the record link (`hubspot-get-link`).

---

## Direction B — OUTBOUND (CRM context → GTM action memo)

**Goal:** read a contact's full CRM context and synthesize a GTM action memo an
SDR/BDR can act on immediately.

### Steps

1. **Resolve the entity.** `hubspot-search-objects` on `contacts` by name/email
   to get the object ID. If ambiguous, ask which one.

2. **Gather context (read-only):**
   - `hubspot-batch-read-objects` for the contact's properties.
   - `hubspot-list-associations` → associated companies and deals.
   - `hubspot-batch-read-objects` on those deals (stage, amount, close date).
   - `hubspot-get-engagement` for recent notes/tasks if IDs are surfaced.

3. **Synthesize the memo** (this is the GTM judgment layer):
   - **Snapshot** — who they are, company, role, lifecycle stage.
   - **Signals** — what the CRM history implies about intent and timing.
   - **Qualification** — ICP fit + a lightweight MEDDIC/BANT read on what's
     known vs. missing (call out the gaps explicitly).
   - **Recommended next action** — the single best next step and why.
   - **Draft outreach** — 2–4 sentence email/message tailored to the context.

4. **Output the memo as text** (do not write to CRM unless asked). Offer to log
   the recommended action as a task via `hubspot-create-engagement`.

---

## Direction C — RETAIL LAUNCH READINESS (partner/channel signal + launch tracker → GTM action memo)

**Goal:** turn messy retail partner and SKU launch signals into a clean
channel-readiness view: risk, owner, next action, escalation, and human
checkpoint before any HubSpot write.

Use this direction when the user mentions retail partners, channel readiness,
launch risk, SKU, pricing, product copy, image assets, inventory, sell-through,
or a weekly business review.

### Embedded demo tracker

**Channels**

| Channel | Status | Owner | Risk | Next action |
| --- | --- | --- | --- | --- |
| Amazon | Ready | Sales Ops | Low | Monitor launch-week sell-through |
| Best Buy | At Risk | Channel Marketing | Asset gap | Confirm lifestyle images and promo price |
| Walmart | Needs QA | Ops | Inventory timing | Verify inbound units before launch date |
| Target | Blocked | Product | Spec mismatch | Resolve product copy and UPC mismatch |

**SKUs**

| SKU | Product | Forecast | Actual | Stock | Risk |
| --- | --- | ---: | ---: | ---: | --- |
| A2579 | Nano USB-C Charger | 1,240 | 1,080 | 3,600 | Low |
| A1688 | Power Bank 10K | 920 | 780 | 880 | Medium |
| T8210 | Smart Camera | 650 | 710 | 420 | High |
| A3947 | Wireless Earbuds | 1,100 | 990 | 1,900 | Low |

### Steps

1. **Parse the launch signal.** Extract channel, SKU/product, issue type
   (asset gap, pricing mismatch, product-data mismatch, inventory risk,
   sell-through risk, task delay), owner if known, urgency, and requested
   action.

2. **Match to the demo tracker.** If the signal introduces a new fact, label it
   as a new signal rather than pretending it was already stored.

3. **Synthesize the memo.**
   - Channel readiness table.
   - SKU risks.
   - Owner and next action.
   - Escalation needed now vs monitor.
   - Human checkpoint before any CRM update.

4. **If asked to log a launch risk**, propose the exact HubSpot note/task/contact
   update and wait for confirmation before writing. If the current tool set
   cannot log notes, output the exact note text for human review.

### Guardrails

- Do not claim pricing-strategy ownership, formal demand-planning ownership,
  retail account ownership, or product-launch ownership.
- The proof is coordination judgment: clean facts, risk, owner, next action,
  escalation, and a human checkpoint.

---

## HubSpot MCP Tools Reference

Read: `hubspot-list-objects`, `hubspot-search-objects`,
`hubspot-batch-read-objects`, `hubspot-list-associations`,
`hubspot-get-association-definitions`, `hubspot-list-properties`,
`hubspot-get-property`, `hubspot-get-engagement`, `hubspot-get-schemas`,
`hubspot-get-user-details`, `hubspot-get-link`, `hubspot-list-workflows`,
`hubspot-get-workflow`.

Write (always confirm first): `hubspot-batch-create-objects`,
`hubspot-batch-update-objects`, `hubspot-batch-create-associations`,
`hubspot-create-engagement`, `hubspot-update-engagement`,
`hubspot-create-property`, `hubspot-update-property`.

Objects are addressed by `objectType` (`contacts`, `companies`, `deals`,
`tickets`, ...) plus properties — there is no per-object tool; pass the type.

---

## Property Reference (demo mapping)

**Contact** (`contacts`): `email`, `firstname`, `lastname`, `jobtitle`,
`phone`, `company`, `lifecyclestage`
(`lead`|`marketingqualifiedlead`|`salesqualifiedlead`|`opportunity`),
`hs_lead_status` (`NEW`|`OPEN`|`IN_PROGRESS`|`CONNECTED`|`OPEN_DEAL`).

**Company** (`companies`): `name`, `domain`, `industry`, `numberofemployees`.

**Deal** (`deals`): `dealname`, `amount`, `dealstage`, `pipeline`,
`closedate`.

Always call `hubspot-list-properties` for the target object before a first
write in a new session — property internal names and enum options vary by
portal.

---

## Safety & conventions

- **Confirm before every write.** Show the exact object + properties first.
  The MCP write tools carry data-modification guardrails; respect them.
- **Never delete.** This skill does not perform delete operations.
- **Deduplicate before create** to avoid polluting the CRM.
- **Record the "why".** Always preserve qualification or launch-risk reasoning
  — that is the GTM judgment the CRM or tracker can't infer on its own.
- Demo portal contains 5 contacts (Jamie Chen, Sarah Kim, David Martinez,
  Lisa Wang, Alex Nguyen) and 3 deals — use them for end-to-end dry runs.

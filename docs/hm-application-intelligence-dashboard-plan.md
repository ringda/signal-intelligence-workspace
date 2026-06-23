# HM Application Intelligence Dashboard 規格計畫

Status: planning spec  
Scope: 新增 Hiring Manager 可讀的 application intelligence dashboard 藍圖  
Default route: `/application-intelligence`  
Last reviewed: 2026-06-23

## 主題

我把高噪音 job market 變成一條可判斷、可驗證、可回寫、會學習的 decision pipeline。

這份文件不是 UI 實作規格，也不是 DB schema 變更提案。它先定義一個 HM-facing dashboard 應該怎麼講清楚這套系統的價值：大量雜訊先被收斂成可判斷的候選池，值得投入的角色才進入 application pipeline；每個判斷都有 evidence，可以被人 review，可以在安全邊界內 write back，最後再把結果轉成下一輪 learning。

## 資料來源與讀取邊界

本規格依據以下來源整理：

- `AGENTS.md`
- `README.md`
- `docs/application-tracker-product-spec.md`
- `../xin-job-hunting/AGENTS.md`
- `../xin-job-hunting/.specify/SYSTEM-SPEC.md`
- `../xin-job-hunting/shared/ssot-core/job-system-live-case.md`

資料邊界：

- Signal Intelligence Workspace 是 interview leverage first 的 Blazor/Telerik prototype；不要把它擴張成 broad platform。
- Application Tracker 可以 read-only 讀取 job-search system of record：`core.jobs`、`core.applications`、`core.descriptions`。
- Dashboard 規格可以描述 governed writeback model，但 MVP 不做自動寫回、不新增寫入 API、不碰 secrets。
- `core.jobs` 是 pre-init 世界，`core.applications` 是 post-init 世界，`core.descriptions` 是 JD 原文；dashboard 不應把舊欄位或 derived view 寫成 canonical truth。
- 若未來呈現 live 數字，必須標示為 snapshot，並由 runtime query 更新；不要把一次性數字寫成永久成果。
- LinkedIn contact names、message content、private outreach detail、secrets、tokens、user-secrets 都不進 HM-facing 頁面。

## Dashboard Thesis

這個 dashboard 要讓 HM 在 60 秒內看懂三件事：

1. 這不是「用 AI 大量投履歷」，而是把判斷規模化。
2. AI 負責跑量、整理、起草；人的判斷負責 rubric、review、claim boundary、writeback gate。
3. 每一次 rejected、reply、interview、stalled follow-up 都能回到系統，讓下一輪 decision baseline 變高。

推薦一句話：

> I turn a noisy job market into a governed decision pipeline: AI runs the volume, evidence keeps each decision inspectable, and human-reviewed writeback makes the next run smarter.

中文意思：

> 我把高噪音 job market 變成一條有治理的 decision pipeline：AI 跑量，證據讓每個判斷可檢查，人把關後再回寫，讓下一輪從更高基線開始。

## 目標讀者與成功標準

目標讀者：

- Hiring Manager：想知道這個候選人是否能把混亂訊號整理成可行動工作流。
- GTM / RevOps / Marketing Ops / CS Ops 類讀者：關心 CRM hygiene、handoff readiness、pipeline discipline、source quality、follow-up rhythm。
- AI-enabled workflow 讀者：關心 AI 是否有邊界、可驗證、可控，而不是只會產生文字。

成功標準：

- HM 不需要知道 repo 內部 skill / command 名稱，也能看懂系統做了什麼。
- 頁面主訊息不是 application volume，而是 decision quality、evidence chain、readiness、learning loop。
- 每個數字都能回答「這幫我做什麼判斷？」而不是只展示資料庫裡有什麼。
- 每個 AI claim 都有明確邊界：AI assists, human decides, writeback is gated。
- Dashboard 可以作為後續 Telerik Blazor 實作的產品規格，不需要 implementer 再決定資訊架構。

## 頁面架構

### 1. Hero Thesis

目的：第一眼建立產品敘事，而不是做 landing page。

內容：

- H1：`Application Intelligence Dashboard`
- Subline：`Turning a noisy job market into a governed, evidence-backed decision pipeline.`
- 三個 proof chips：
  - `High-noise market -> qualified pipeline`
  - `Evidence-backed decisions`
  - `Human-gated writeback`

避免：

- 不寫成「我自動化求職」。
- 不把 20,000+ 類數字放成無邊界的英雄成就。
- 不使用 marketing-style 空泛形容；第一屏要直接進入 workflow。

### 2. Signal Funnel

目的：讓 HM 看到「大量訊號如何被收斂」，而不是只看到投遞數。

建議呈現：

- `Market signals reviewed by AI/rubric`
- `Recommended / watch / not recommended`
- `Human-confirmed apply slate`
- `Applications submitted`
- `Outcomes observed`

資料來源：

- Pre-init：`core.jobs`
- Post-init：`core.applications`
- JD text：`core.descriptions`
- 若使用 `job-system-live-case.md` 的量級說法，必須保留 claim boundary：AI-screened job rows，不是人工逐條讀過。

判斷重點：

- Funnel 要強調 filtering quality，不鼓勵 blind volume。
- 高 application count 但 weak fit、no tailoring、no follow-up 不應被視為成功。

### 3. Decision Pipeline

目的：把求職流程翻成可 inspect 的 pipeline。

建議步驟：

```text
shortlist -> role decision -> people-calibrated routing
          -> resume tailoring -> apply
          -> outreach / referral -> follow-up
          -> result review -> weekly learning loop
```

對應系統邊界：

- 沒有 application row：pre-init candidate pool。
- 有 application row：post-init application pipeline。
- People-Calibrated Routing 完成才代表 role contract 清楚。
- Resume Tailor、Resume Export、CL、Applied、Status Sync 都應對應 canonical milestone 或 lifecycle state。

畫面可以呈現：

- 每一步的 done / in progress / blocked / skipped count。
- 每一步缺的下一個 action。
- 每一步可以回查的 evidence source。

### 4. Evidence + Verification

目的：證明每個判斷不是黑箱。

建議欄位：

- Company / role title
- Current stage
- Fit / risk summary
- Correct pool
- Must prove
- Cannot borrow / claim ceiling
- Resume state
- Outreach state
- Next action
- Evidence links：JD、routing brief、tailor analysis、application folder

設計原則：

- Evidence panel 優先呈現「為什麼這個角色值得或不值得」。
- `cannot_borrow` 是 claim boundary，不是缺點清單；它保護履歷與面試可防守性。
- Private outreach 只能摘要狀態，不展示私人訊息全文。

### 5. Case Microscope

目的：用一個 application case 展示整條 decision chain。

建議 case 內容：

- Role snapshot：公司、職稱、role loop、priority。
- JD evidence：這份角色真正要求的 work object。
- Routing decision：correct pool、wrong pool、must prove、cannot borrow。
- Tailoring decision：哪些經驗可用、哪些不能借。
- Apply / outreach state：目前是否已投遞、是否開始 networking、是否有 follow-up。
- Learning hook：這個 case 對後續定位、resume、outreach 或 source quality 造成什麼學習。

展示邊界：

- 不顯示未授權的 LinkedIn 個人資訊。
- 不顯示私人訊息內容。
- 不把單一 case 包裝成 general market proof。

### 6. Learning Loop

目的：說明這套系統會學習，不只是紀錄。

建議呈現：

- Rejections by role type / source / fit risk。
- Replies and conversations by outreach pattern。
- Stalled applications needing follow-up。
- Weekly lessons：哪些 source、role lane、message hook、resume angle 需要調整。
- Writeback queue：哪些洞察只是一週觀察，哪些值得進入 spec / skill / SSOT。

治理原則：

- Learning 不等於自動改規則；需要 human review。
- 一次性 insight 不直接升級成長期規則。
- 只有能防止 drift、overclaim、wrong pool、漏回寫或重複失敗的 learnings 才值得進 durable source。

## MVP Scope

MVP 是一份 dashboard blueprint 加上一個後續可實作的 read-only page plan。

包含：

- 新頁規格：`/application-intelligence`。
- HM-facing thesis、page architecture、metrics map、claim boundary。
- Read-only aggregate-first 設計。
- Case microscope 的資料需求與展示邊界。
- Sprint plan 與驗證方式。

不包含：

- 不改 `src/SignalIntelligenceWorkspace/Components/Pages/Cockpit.razor`。
- 不改 `src/SignalIntelligenceWorkspace/Services/Cockpit/CockpitDataService.cs`。
- 不新增 DB 欄位、migration、view 或 write API。
- 不碰 user-secrets、tokens、HubSpot private app token、AI provider keys。
- 不展示私人 LinkedIn contact names 或 message content。
- 不把 `/cockpit` 重新設計成這個頁面；`/cockpit` 維持 operational cockpit，`/application-intelligence` 才是 HM-facing narrative page。

## 資料欄位與可呈現指標

### Source-of-truth map

| 概念 | Canonical source | Dashboard 用法 |
|---|---|---|
| Pre-init job pool | `core.jobs` | 市場訊號、初篩、recommendation 狀態 |
| JD 原文 | `core.descriptions` | role evidence、work object、風險判斷 |
| Post-init application | `core.applications` | pipeline stage、milestone、application lifecycle |
| Application artifacts | `80-applications/{folder}/` in job-search repo | case microscope 的 evidence links |
| Routing evidence | `routing_brief.md` | correct pool、must prove、cannot borrow |
| Tailoring evidence | tailor analysis / resume artifacts | claim 是否可防守 |
| Outreach evidence | networking artifacts | coverage / status 摘要，不展示私人內容 |

### Metrics map

建議 MVP metrics：

- Market signals screened：只寫 AI/rubric screened，不寫人工逐條讀。
- Recommended roles：顯示 recommendation quality，不只顯示總量。
- Human-confirmed slate：顯示哪些角色真的進入 application pipeline。
- Applications submitted：搭配 tailoring / follow-up 狀態看，不單獨當成功。
- Tailored resumes completed：顯示 role-specific work。
- Outreach started：顯示 coverage，不展示私人訊息。
- Follow-ups due：顯示 next action discipline。
- Outcomes observed：rejected、interview、reply、no response、closed。
- Weekly learning notes：顯示 system improvement，不寫成自動學習。

避免 metrics：

- 不用 application volume 當主 KPI。
- 不把 AI-generated draft count 當成果。
- 不把 commit 數寫成規則修正次數；若使用 commit 數，只能說明 versioned iteration scale。

## Claim Boundary

可講：

- 這是 self-built portfolio / interview demo 規格。
- 系統把 noisy job market 轉成 governed decision pipeline。
- AI 協助篩選、整理、起草、比較；人定 rubric、review、決定、把關 writeback。
- Application Tracker 可從 real configured job-search system of record 讀取資料。
- Dashboard 顯示的是 job-market fit / winnability / readiness signal。
- 系統設計重點是 judgment scaling，不是 blind automation。

不可講：

- 不說這是 production AI platform。
- 不說這是正式 HubSpot 或任何公司內部 CRM/GTM 系統。
- 不說 AI 自動投遞、白名單外寫回、或無人審核決策。
- 不說 job-market signal 是 buyer intent。
- 不說自己人工讀過所有 AI-screened job rows。
- 不把 private outreach、secrets、tokens、個人訊息放到 demo。

推薦 HM-safe wording：

> This is a portfolio prototype built from my own live application workflow. It shows how I structure noisy market signals into evidence-backed decisions, keep AI inside review boundaries, and use outcomes to improve the next run.

中文意思：

> 這是我用自己的真實求職工作流整理出的 portfolio prototype。它展示我如何把雜訊市場訊號變成有證據支撐的判斷，把 AI 留在 review boundary 內，並把結果回饋到下一輪。

## 後續 Sprint Plan

### Sprint 0：文件與 claim boundary

目標：

- 建立本文件。
- 鎖定 `/application-intelligence` 的產品敘事與頁面架構。
- 確認不改 UI、不改 DB、不碰 secrets。

交付：

- `docs/hm-application-intelligence-dashboard-plan.md`
- 明確的資料來源、MVP scope、claim boundary、sprint plan。

驗收：

- `dotnet build`
- `dotnet test`

### Sprint 1：靜態 dashboard prototype

目標：

- 新增一個可視覺檢查的 static / mock dashboard prototype。
- 先用 hand-authored sample data，不接 DB。

交付：

- 可在瀏覽器檢查的 `/application-intelligence` 或 static prototype。
- Hero、Signal Funnel、Decision Pipeline、Evidence Panel、Case Microscope、Learning Loop 六區。

驗收：

- Telerik 元件與 layout 實作前先查 Telerik MCP / official docs。
- 桌機與手機 viewport 不重疊、不溢字。
- 不出現 private data。

### Sprint 2：Read-only live aggregate

目標：

- 在不改 DB schema 的前提下，接 read-only aggregate。
- 只呈現 aggregate 與 safe snapshot。

交付：

- Aggregate service/query plan。
- Data freshness / snapshot label。
- Missing data state 與 fallback state。

驗收：

- 不新增 write path。
- 不讀 secrets 到前端。
- 若資料不足，畫面明確顯示 unavailable，而不是假裝完整。

### Sprint 3：Case Microscope

目標：

- 選一個 demo-safe application case，展示 evidence chain。

交付：

- Case summary。
- JD evidence。
- Routing decision。
- Tailoring proof。
- Application / outreach status summary。
- Claim ceiling display。

驗收：

- 不展示私人 contact names / messages。
- 每個 claim 都能回到 artifact 或 canonical field。
- 不把單案說成普遍成果。

### Sprint 4：Learning Loop 與 HM demo script

目標：

- 把 dashboard 變成可口述的 HM demo。
- 將 weekly learning 與 writeback gate 講成工作能力。

交付：

- HM demo script。
- Learning loop panel。
- Safe wording bank：T1 / T2 / T3 deepness variants。

驗收：

- Demo 可以在 3 分鐘內說完。
- 可根據 JD 決定講深或講淺。
- 不使用後台 skill / command 名稱當前台賣點。

## 驗證方式與交付定義

本次文件建立的完成定義：

- 文件落地：`docs/hm-application-intelligence-dashboard-plan.md` 存在。
- Scope 守住：只新增文件，不改 UI、不改 DB、不碰 secrets。
- Repo 驗證：`dotnet build` 與 `dotnet test` 成功，或明確回報 blocker。
- 後續可接續：下一手可以直接依此文件實作 `/application-intelligence`，不需要重新決定頁面架構、MVP scope 或 claim boundary。

後續若進入 Telerik Blazor 實作，才需要：

- 使用 Telerik MCP / official docs 驗證 component API。
- 建立 route / page / styles。
- 啟動 app 並 browser verify `/application-intelligence`。
- 檢查 console、layout、chart/grid render、mobile fit。

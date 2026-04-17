# Step 6 Sprint Execution Board v1

## Metadata
- Version: v1
- Date: 2026-04-08
- Source backlog: `Governance/Backlog/STEP5_EXECUTION_BACKLOG_v1.csv`

## Planning Assumptions
1. Team runs 1-week sprints (grouped below into delivery waves).
2. Governance dependencies are mandatory and cannot be bypassed.
3. P0 tickets must complete before structural work begins.

## Sprint Wave Plan

### S1 (Week 1-2)
Goal: establish consistency and governance foundations.
1. QW-01 Unify status badges and action hierarchy
2. QW-02 Inline validation standard + blocking summary
3. QW-03 Reason code capture for governed decisions
4. QW-06 Audit payload completeness validator

Exit criteria:
1. Core UI consistency patterns are active.
2. Governed decisions enforce reason code policy.
3. Audit payload completeness checks pass for covered actions.

### S2 (Week 3)
Goal: reduce operator friction and harden critical execute gating.
1. QW-04 Persistent search/filter/sort presets
2. QW-05 Dashboard warning cards
3. QW-07 Restore/rollover preflight execute gate

Exit criteria:
1. Table preferences persist within session.
2. Dashboard surfaces operational warning signals.
3. Tier3 operations are execute-gated by preflight.

### S3 (Week 4-5)
Goal: consolidate workflow context into primary work surfaces.
1. ME-01 Consistent list-detail workspace pattern
2. ME-02 Enrollment decision workbench

Exit criteria:
1. Enrollment decisions happen in one work surface.
2. Modal fragmentation reduced for primary module tasks.

### S4 (Week 6)
Goal: make requirements and queue quality first-class.
1. ME-03 Unified requirement checklist component
2. ME-06 Queue aging/SLA indicators

Exit criteria:
1. Requirement statuses are consistent across student and enrollment contexts.
2. Enrollment queue exposes age/severity indicators.

### S5 (Week 7)
Goal: harden archive, reports, and restore operational behavior.
1. ME-04 Archive/restore dependency impact preview
2. ME-05 Reports presets + run history
3. ME-07 Restore conflict and forced session refresh

Exit criteria:
1. Archive/restore actions show dependency impact before commit.
2. Report runs are reusable via presets.
3. Restore success forces safe session refresh behavior.

### S6 (Week 8-9)
Goal: deliver policy-driven transition governance and permission scaffolding.
1. SR-01 Enrollment state machine service
2. SR-02 Permission boundary layer

Exit criteria:
1. Enrollment transitions are centrally policy-enforced.
2. Permission keys cover governed operations with SUPERADMIN pass-through intact.

### S7 (Week 10-11)
Goal: ship reusable preflight safety framework.
1. SR-03 Shared preflight framework

Exit criteria:
1. Restore and rollover use common preflight contracts and result models.

### S8 (Week 12+)
Goal: close loop on analytics, exception handling, and traceability.
1. SR-04 Operational metrics dashboard service
2. SR-05 Exception queue architecture
3. SR-06 Correlation/request-id propagation

Exit criteria:
1. Operational trends are measurable from system telemetry.
2. Edge-case failures are managed via structured exception queue.
3. Governed actions are traceable end-to-end by correlation id.

## Dependency Rules (Hard)
1. `QW-03` must complete before `ME-02` and `SR-01`.
2. `ME-01` must complete before `ME-02`.
3. `QW-07` must complete before `ME-04` and `ME-07`.
4. `ME-02` and `QW-03` must complete before `SR-01`.
5. `SR-01` must complete before `SR-04` and `SR-05`.

## Sprint-Level QA Focus
1. S1-S2:
   - reason code enforcement
   - audit payload completeness
   - validation timing consistency
2. S3-S5:
   - workflow completion speed
   - decision accuracy
   - failure recovery behavior
3. S6-S8:
   - transition governance correctness
   - permission boundary readiness
   - traceability and observability

## KPI Checkpoints
1. End of S2:
   - confirm trend improvement in form errors and decision compliance.
2. End of S5:
   - compare baseline against rework/queue-aging metrics.
3. End of S8:
   - validate strategic KPI targets against Step 1 baseline.

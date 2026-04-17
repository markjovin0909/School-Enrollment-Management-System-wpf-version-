# Step 5 Execution Backlog v1

## Metadata
- Version: v1
- Date: 2026-04-08
- Source ticket list: `STEP5_EXECUTION_BACKLOG_v1.csv`
- Scope lock reference: `Governance/Baseline/STEP1_SCOPE_LOCK.md`

## Delivery Model
1. Phase A: Quick Wins (stability and consistency)
2. Phase B: Medium Effort (workflow acceleration)
3. Phase C: Structural Redesign (governed scalability)

## Phase A: Quick Wins
1. QW-01 `Unify status badges and action hierarchy` (P0)
2. QW-02 `Inline validation standard + blocking summary` (P0)
3. QW-03 `Reason code capture for governed decisions` (P0)
4. QW-04 `Persistent search/filter/sort presets` (P1)
5. QW-05 `Dashboard warning cards` (P1)
6. QW-06 `Audit payload completeness validator` (P1)
7. QW-07 `Restore/rollover preflight execute gate` (P1)

### Phase A exit criteria
1. UI consistency rules enforced in major modules.
2. Governed decisions cannot be committed without required controls.
3. High-risk execute actions are gated by preflight.

## Phase B: Medium Effort
1. ME-01 `Consistent list-detail workspace pattern` (P0)
2. ME-02 `Enrollment decision workbench` (P0)
3. ME-03 `Unified requirement checklist component` (P0)
4. ME-04 `Archive/restore dependency impact preview` (P1)
5. ME-05 `Reports presets + run history` (P1)
6. ME-06 `Enrollment queue aging/SLA indicators` (P1)
7. ME-07 `Restore conflict + forced session refresh` (P1)

### Phase B exit criteria
1. Enrollment decisions are context-complete in one workbench.
2. Requirements become first-class gating signals.
3. Archive/restore and reporting flows reduce repeat effort and misuse risk.

## Phase C: Structural Redesign
1. SR-01 `Enrollment state machine service` (P0)
2. SR-02 `Permission boundary layer` (P0)
3. SR-03 `Shared preflight framework` (P0)
4. SR-04 `Operational analytics service` (P1)
5. SR-05 `Exception queue architecture` (P1)
6. SR-06 `Correlation/request-id propagation` (P1)

### Phase C exit criteria
1. Enrollment transitions are policy-enforced centrally.
2. Future role expansion enabled without major refactor.
3. Critical operations share reusable safety controls.

## Suggested Sequencing (8-week example)
1. Week 1-2:
   - QW-01, QW-02, QW-03, QW-06
2. Week 3:
   - QW-04, QW-05, QW-07
3. Week 4-5:
   - ME-01, ME-02
4. Week 6:
   - ME-03, ME-06
5. Week 7:
   - ME-04, ME-05, ME-07
6. Week 8+:
   - SR-01, SR-02, SR-03
   - then SR-04, SR-05, SR-06

## Dependency Highlights
1. QW-03 is a hard dependency for most governance-sensitive work.
2. ME-02 depends on ME-01 and QW-03.
3. SR-01 depends on ME-02 + QW-03.
4. SR-03 depends on QW-07 and ME-07.

## Definition of Done (per ticket)
1. Behavior implemented per screen/flow specs.
2. Automated or repeatable manual test cases updated.
3. Audit payload validated for governed actions.
4. Failure path verified (state unchanged or rollback as designed).
5. Documentation updated if user-facing workflow changes.

## KPI Alignment to Baseline
1. Track against Step 1 baseline:
   - task-time proxies
   - error rate
   - rollback incidents
   - support issues
2. Evaluate KPI delta after each phase, not only at end-state.

# Step 4 Risk Control Matrix v1

## Metadata
- Version: v1
- Date: 2026-04-08
- Source matrix: `STEP4_RISK_CONTROL_MATRIX_v1.csv`
- Scope model: `SUPERADMIN-only` (future role boundaries defined as forward contract)

## Confirmation Tier Model
1. Tier0: No extra confirmation. Used for low-risk/non-destructive actions.
2. Tier1: Standard confirmation (or explicit save intent). No typed phrase.
3. Tier2: Guarded confirmation + reason code required.
4. Tier3: Highest risk. Typed confirmation + reason code + preflight pass required.

## Minimum Audit Payload Contract
All governed actions must capture:
1. `actor_user_id`
2. `action_key`
3. `entity` + `entity_id`
4. `timestamp_utc`
5. `result` (success/fail)
6. `before`/`after` snapshot or `status_from`/`status_to`
7. `reason_code` when required
8. `correlation_id` (recommended for Step 5+)

## High-Risk Action Controls (Critical/High)
### Enrollment
1. `approve_enrollment`:
   - Tier2
   - reason code required
   - commit-time capacity + concurrency recheck
   - failure fallback: keep `Under Review`
2. `reject_enrollment`:
   - Tier2
   - reason code required
   - governed reopen only
3. `explicit_status_transition`:
   - Tier3
   - typed confirmation + reason code
   - override note mandatory

### Archive/Restore
1. `archive_record`:
   - Tier2
   - dependency precheck mandatory
   - reason code required
2. `restore_record`:
   - Tier2
   - conflict/dependency precheck mandatory
   - reason code required

### Maintenance
1. `run_restore`:
   - Tier3
   - typed confirmation + reason code
   - preflight and dependency checks mandatory
   - session invalidation required on success
2. `execute_year_end_rollover`:
   - Tier3
   - typed confirmation + reason code
   - preflight + impact preview mandatory
   - transactional rollback required
3. `db_settings_update`:
   - Tier3
   - typed confirmation + reason code
   - rollback to last known-good config required

## Rollback Strategy Rules
1. Workflow decisions:
   - reversible only through governed transitions and full audit.
2. Maintenance operations:
   - prefer atomic transaction rollback.
3. Security operations:
   - compensating actions (reset again, revoke tokens), not silent rollback.
4. Archive/restore:
   - archive has restore path; restore has re-archive path.

## Failure Default Rules
1. On any validation or preflight failure:
   - keep current system state unchanged.
2. On concurrency conflict:
   - reject commit and require refresh/reapply.
3. On partial maintenance failure:
   - auto rollback when transaction-enabled; if not, enter incident state and block further destructive actions.

## Permission Boundary (Now vs Future)
1. Now:
   - all governed actions: SUPERADMIN
2. Future boundary map:
   - Admissions/Registrar: enrollment decisions
   - Compliance: requirement verification/rejection
   - Data Manager: archive/restore
   - Ops Admin: backup/restore
   - Principal/System Owner: Tier3 approvals

## Required UI Patterns by Risk
1. Tier0/Tier1:
   - inline page actions and standard dialogs
2. Tier2:
   - guarded confirmation dialog with reason code input
3. Tier3:
   - wizard or guarded dialog with:
     - preflight summary
     - typed confirmation input
     - explicit impact statement

## Implementation Notes
1. This matrix is the single control source for:
   - confirmation behavior
   - audit completeness checks
   - action enable/disable gating
2. Any new action must be added here before implementation.

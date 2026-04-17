# Baseline Metrics Specification (Step 1)

## Metric Groups
1. Task Time
2. Error Rate
3. Rollback Incidents
4. Support Issues

## 1) Task Time
### A. Session Duration Proxy (System-derived)
- Source: `audit_logs`
- Definition: time from `LOGIN_SUCCESS` to next `LOGOUT` for same user
- Output:
  - session count
  - avg seconds
  - min seconds
  - max seconds

### B. Enrollment Cycle Proxy (System-derived)
- Source: `enrollments`
- Definition: `created_at` to `approved_at` (or `updated_at` if not approved)
- Output:
  - enrollment count in baseline window
  - avg minutes

### C. Task Study (Operator-measured, optional but recommended)
- Source: `TaskTimeStudy.csv`
- Definition: observed start/end times for explicit tasks
- Output:
  - avg duration by workflow
  - failure/retry markers

## 2) Error Rate
- Source: `audit_logs`
- Numerator:
  - actions containing `FAILED` or `ERROR`
  - explicit `LOGIN_FAILED`
- Denominator:
  - all audit actions in baseline window
- Output:
  - total actions
  - error actions
  - percent error rate

## 3) Rollback Incidents
- Source:
  - `audit_logs` (`RESTORE*`, `ROLLBACK*`, `UNDO`, `REOPEN`)
  - `archive_records` (`is_restored = 1`)
- Output:
  - rollback-like audit events
  - archive restore events
  - combined incident count

## 4) Support Issues
- Source priority:
  1. `SupportIssues.csv` (manual/service desk export)
  2. Notification fallback (`%APPDATA%/.../Notifications/notifications.json`)
- Output:
  - opened issues in window
  - unresolved issues in window
  - fallback detected issue signals

## Reporting Artifacts
1. `baseline_YYYYMMDD_HHMMSS.json` (machine-readable)
2. `baseline_YYYYMMDD_HHMMSS.md` (human-readable)
3. `audit_action_distribution_YYYYMMDD_HHMMSS.csv`
4. `enrollment_status_distribution_YYYYMMDD_HHMMSS.csv`

## Notes
1. Task time from audit logs is a proxy, not full UX timing.
2. For redesign ROI, maintain weekly snapshots and compare trend lines post-release.

# Step 3 Screen Behavior Spec v1

## Metadata
- Version: v1
- Date: 2026-04-08
- Depends on:
  - `Governance/Baseline/STEP1_SCOPE_LOCK.md`
  - `Governance/FlowPack/STEP2_ASIS_TOBE_FLOW_PACK_v1.md`

## Global Interaction Contract
1. Primary action appears once per screen section.
2. Destructive actions are visually separated and never default-focused.
3. Inline validation appears on blur/change; blocking summary appears on submit.
4. All governed actions require confirmation tier and audit logging.
5. Long workflows use in-page panels; avoid modal stacking.
6. Every screen supports explicit states: `Empty`, `Loading`, `Error`, `Success`.

## Global Validation Timing
1. `Field-level` validation: on blur and on submit.
2. `Cross-field` validation: on submit and when dependent fields change.
3. `Server policy` validation: pre-commit only, with clear blocking message.
4. `Concurrency` validation: commit-time only (version or stale-data check).

## Global Table Contract
1. Sticky top toolbar: search, filters, refresh, optional bulk actions.
2. Sort and filter persistence per user session.
3. Row selection updates detail panel; no hidden side effects.
4. Bulk actions only for valid selected rows; disabled otherwise with reason tooltip.
5. Empty table state must explain how to recover (clear filters / add record).

---

## Screen: Dashboard
### Primary Views
1. KPI cards: Students, Teachers, Sections, Offerings, Enrolled, Pending.
2. Alert summary: pending requirements, waitlist pressure, critical failures.
3. Recent activity feed.

### Secondary Views
1. Quick links to enrollment review, requirements, backup/restore logs.

### Key Actions
1. `Refresh Dashboard` (enabled always, throttled).
2. `Open Queue` links from KPI cards.

### Empty State
1. Show "No recent activity yet" with onboarding links.

### Loading State
1. Skeleton cards + loading rows.

### Error State
1. Partial fail allowed: cards or activity can fail independently.
2. Show inline retry button per failed panel.

### Success Feedback
1. `Updated at HH:mm:ss` indicator.

### Audit Touchpoints
1. No special audit for read-only refresh.

---

## Screen: Students
### Primary Views
1. Student table (searchable/filterable).
2. Student detail form.

### Secondary Views
1. Account state block.
2. Requirements snapshot.
3. Activity/history panel.

### Key Actions
1. `Add` (always enabled).
2. `Save` (enabled when dirty + valid minimum fields).
3. `Archive/Restore` (enabled on selected record; tiered confirmation).
4. `Reset Account` (enabled on selected record with linked account).
5. `Clear` (enabled when form dirty).

### Field Behavior
1. `Student No`: required, unique, read-only after create unless override.
2. `LRN`: required by policy, uniqueness check on blur.
3. Name fields: required for first/last; middle optional.
4. `Birthdate`: required; rejects future date.
5. `Status`: controlled enum only.

### Table Behavior
1. Quick filters: `All`, `Active`, `Inactive`, `Archived`.
2. Search matches student no, LRN, account id, first/last name.
3. Selecting row loads detail without opening modal.

### Empty State
1. "No students found" + `Add Student` CTA.

### Loading State
1. Table loads first, detail pane lazy-loads.

### Error State
1. Inline field errors + top summary for blocking submit.
2. Duplicate conflict shows linkable existing record.

### Success Feedback
1. Toast: `Student saved` with record id.

### Audit Touchpoints
1. Create/update/archive/restore/reset account actions audited with actor + entity id.

---

## Screen: Teachers
### Primary Views
1. Teacher table.
2. Teacher detail form.

### Secondary Views
1. Account/security state.
2. Load summary panel.

### Key Actions
1. `Add`, `Save`, `Archive/Restore`, `Reset Password`, `Clear`.

### Field Behavior
1. `Employee No`: required and unique.
2. Name fields: required first/last.
3. Email/contact: format checked on blur.
4. Account status controls visible and explicit.

### Table Behavior
1. Search by employee no, account id, name, status.
2. Quick filters for active/inactive/locked.

### Empty/Loading/Error/Success
1. Same contract as Students with teacher-specific copy.

### Audit Touchpoints
1. Create/update/archive/restore/reset password audited.

---

## Screen: Enrollment (Decision Workbench)
### Primary Views
1. Queue table by status.
2. Enrollment detail panel.
3. Decision/action panel.

### Secondary Views
1. Requirement checklist.
2. Capacity and schedule conflict panel.
3. Transition history timeline.

### Key Actions
1. `Submit Enrollment`
2. `Approve`
3. `Return for Correction`
4. `Reject`
5. `Waitlist`
6. `Promote Waitlist`
7. `Explicit Status Transition` (admin-only, reason mandatory)

### Action Enablement Rules
1. `Approve` enabled only when status allows and requirements complete.
2. `Return`/`Reject` require reason code before button enabled.
3. `Waitlist` enabled only for eligible statuses.
4. `Promote Waitlist` enabled when waitlist exists and seat available.

### Validation Contract
1. One active enrollment per student-year.
2. Year open and not archived.
3. Section/curriculum/grade mapping valid.
4. Commit-time revalidation for capacity/conflicts/stale record.

### Queue/Table Behavior
1. Tabs: Draft, Submitted, Under Review, Pending Requirements, Ready, Waitlisted, Approved, Rejected, Cancelled, Archived.
2. Columns include aging and conflict indicators.
3. Row select updates workbench; retains active filter.

### Empty State
1. Per-status message with clear filter reset action.

### Loading State
1. Incremental queue loading.

### Error State
1. Stale-data conflict panel with `Reload and Reapply`.
2. Capacity conflict panel with alternative section suggestions.

### Success Feedback
1. Decision toast + optional `Open Next Item`.

### Audit Touchpoints
1. Every transition logs from/to status, reason code, actor, timestamp, entity id.

---

## Screen: Reports
### Primary Views
1. Report type selector.
2. Dynamic filter panel.
3. Result grid.

### Secondary Views
1. Saved presets.
2. Run history and export history.

### Key Actions
1. `Load Report`
2. `Save Preset`
3. `Export CSV`

### Validation Contract
1. Required filters depend on report type.
2. Invalid filter combinations blocked before run.

### Table Behavior
1. Sortable columns.
2. Optional grouped totals.
3. Row virtualization for large datasets.

### Empty State
1. "Select report and filters to begin."

### Loading State
1. Query progress indicator.

### Error State
1. Query failure banner with retry and filter hints.

### Success Feedback
1. Export success with file path and open action.

### Audit Touchpoints
1. Export action logged with report type and filter summary hash.

---

## Screen: Operations
### Primary Views
1. Domain navigation:
   - Master Data
   - Scheduling
   - Accounts and Compliance
   - Maintenance
2. Module list/grid.
3. Module detail/editor panel.

### Secondary Views
1. Dependency impact panel.
2. Operation history panel (where applicable).

### Key Actions
1. CRUD for setup entities.
2. Archive/restore.
3. Backup/restore execution.
4. Year-end rollover execution.

### Validation Contract
1. All foreign-key relationships validated before save.
2. Unique constraints checked with clear conflict messages.
3. High-risk operations require preflight pass before execute enabled.

### Backup/Restore Behavior
1. `Backup Now` enabled when destination valid.
2. `Restore Now` disabled until artifact selected + acknowledgment checked.
3. On successful restore, force session refresh/re-login.

### Year-End Rollover Behavior
1. Execute button disabled until preflight passes and confirmation complete.
2. Preview panel must show affected counts before execute.

### Empty/Loading/Error/Success
1. Empty state includes setup guidance.
2. Loading state shows per-module skeleton.
3. Error state includes remediation steps (not generic error only).
4. Success state includes operation summary with audit reference.

### Audit Touchpoints
1. Archive/restore/backup/restore/rollover fully audited with reason and outcome.

---

## Login and Session (Support Contract)
### Key Behavior
1. Login supports sign-in, forgot password, DB settings, test connection.
2. Lockout and failed attempts shown with clear cooldown messaging.
3. Session timeout warning then forced logout.

### Required States
1. `Invalid credentials`
2. `Account locked`
3. `DB unreachable`
4. `Session expired`

### Audit Touchpoints
1. `LOGIN_SUCCESS`, `LOGIN_FAILED`, `LOGOUT`, `SESSION_TIMEOUT`.

---

## Implementation Notes for QA
1. Every key action must have deterministic enabled/disabled rules.
2. Every blocking validation must map to exact field or policy message.
3. Each screen must pass empty/loading/error/success state tests.
4. High-risk operations must fail safely and preserve data integrity.

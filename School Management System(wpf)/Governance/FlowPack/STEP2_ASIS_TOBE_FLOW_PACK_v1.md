# Step 2 As-Is vs To-Be Flow Pack (v1)

## Metadata
- Version: v1
- Date: 2026-04-08
- Scope source: `Governance/Baseline/STEP1_SCOPE_LOCK.md`
- Baseline source: `Governance/Baseline/BaselineReports/`
- Applies to current access model: `SUPERADMIN-only`

## Flow 1: Add New Student
### Objective
Create a valid student profile with consistent account linkage and minimal rework.

### Trigger
New applicant/admittee intake.

### As-Is
1. Open Students tab.
2. Fill dense profile form.
3. Save and handle failures post-submit.

### As-Is bottlenecks
1. Late duplicate detection.
2. Large form creates cognitive overload.
3. Account creation failures are not framed as explicit follow-up tasks.

### To-Be
1. Start `New Student` wizard in side panel:
   - Step A: Identity
   - Step B: Contact/Guardian
   - Step C: Academic context
2. Run inline uniqueness checks (Student No, LRN, name+birthdate similarity).
3. Allow `Save Draft` at any step.
4. `Submit` performs atomic create + account provisioning + requirement template assignment.
5. Show completion panel with next actions.

### Decision points
1. Duplicate found: merge, cancel, or force-create (reason required).
2. Activate now or keep pending profile completion.

### Validations
1. Required legal name fields.
2. Unique Student No.
3. LRN format and uniqueness policy.
4. Birthdate plausibility.
5. Guardian contact format.

### Failure cases and recovery
1. Duplicate collision -> block submit; guide to existing record.
2. Account provisioning failure -> persist student as pending account sync; provide retry action.
3. Validation failures -> remain in draft with field-level errors.

### Final state
`Active` or `Pending Completion` student with audit entry.

---

## Flow 2: Add New Teacher
### Objective
Create teacher identity record and controlled account state.

### Trigger
Teacher onboarding.

### As-Is
1. Open Teachers tab.
2. Enter profile and account fields in same form.
3. Save and validate outcome manually.

### As-Is bottlenecks
1. Profile and security concerns mixed in one pass.
2. Weak visibility on staged onboarding states.

### To-Be
1. Split create flow:
   - Step A: Teacher identity/profile
   - Step B: Account/security
2. Validate employee number uniqueness before final submit.
3. Explicit account mode:
   - profile-only (inactive account)
   - active account
4. Post-save state badges and follow-up actions.

### Decision points
1. Active account now vs staged account.
2. Password setup now vs forced reset on first login.

### Validations
1. Unique employee number.
2. Required profile fields.
3. Contact format checks.

### Failure cases and recovery
1. Duplicate employee number -> block and link to existing record.
2. Account policy mismatch -> save profile-only and queue account fix.

### Final state
Teacher created with explicit account status and audit record.

---

## Flow 3: Process New Enrollment Submission
### Objective
Intake enrollment into governed review pipeline.

### Trigger
Enrollment request initiated for student + school year.

### As-Is
1. Select year/student/grade/section/curriculum.
2. Submit.
3. Handle status changes in separate actions.

### As-Is bottlenecks
1. Weakly visible gating between requirements, capacity, and approval readiness.
2. Status edits may feel action-button driven instead of policy-driven.

### To-Be
1. Use `Enrollment Intake` form with readiness indicators.
2. Validate one-active-enrollment rule and mapping constraints before submit.
3. Auto-route on submit:
   - `Submitted`
   - `Pending Requirements`
   - `Waitlisted` (if no seat and policy allows)
4. Create initial decision journal entry.

### Decision points
1. Capacity available?
2. Requirements complete?
3. Mapping/policy constraints satisfied?

### Validations
1. Unique active enrollment per student-year.
2. Active school year only.
3. Valid grade/section/curriculum relationship.

### Failure cases and recovery
1. Duplicate enrollment -> block submit with record link.
2. Invalid mapping -> show corrective choices.
3. Closed year -> block and route to valid year selection.

### Final state
Enrollment in a valid initial status with full audit metadata.

---

## Flow 4: Review / Approve / Reject / Return Enrollment
### Objective
Perform consistent and auditable enrollment decisions.

### Trigger
Reviewer opens item from review queue.

### As-Is
1. Filter grid.
2. Select record.
3. Execute status action buttons.

### As-Is bottlenecks
1. Decision context can be split across screens.
2. Risk of inconsistent reviewer outcomes.
3. Limited stale-data safeguards.

### To-Be
1. Decision Workbench with three panes:
   - student and enrollment summary
   - requirements and policy checks
   - placement/capacity and action controls
2. Require reason code for non-approve outcomes.
3. Commit-time revalidation (capacity + concurrency + policy).
4. Auto-open next queue item after completion.

### Decision points
1. Approve.
2. Return for correction.
3. Reject.
4. Waitlist.

### Validations
1. Transition must be allowed by policy matrix.
2. Commit-time section capacity and schedule conflict checks.
3. Required reason code and optional reason note.

### Failure cases and recovery
1. Stale record version -> force refresh and reconfirm.
2. Seat consumed during approval -> fail approve and keep in review.

### Final state
`Approved`, `Returned for Correction`, `Rejected`, or `Waitlisted` with immutable transition log.

---

## Flow 5: Manage Incomplete Requirements
### Objective
Resolve requirement deficiencies while preserving workflow progress visibility.

### Trigger
Missing/invalid requirement in student or enrollment context.

### As-Is
1. Requirements managed in separate module flow.
2. Reviewer manually aligns requirement state and enrollment state.

### As-Is bottlenecks
1. Requirement state and enrollment gate are loosely coupled.
2. Weak due-date and aging visibility.

### To-Be
1. Standard requirement statuses:
   - Missing
   - Submitted
   - Verified
   - Rejected
   - Expired
2. Expose requirement checklist in both student detail and enrollment workbench.
3. Auto-recompute enrollment readiness from checklist state.
4. Track due date and aging.

### Decision points
1. Hard block vs conditional continuation policy.
2. Reject document vs request correction.

### Validations
1. Requirement-specific accepted proof rules.
2. Verifier identity and timestamp mandatory for verification outcomes.

### Failure cases and recovery
1. Wrong/expired document -> mark rejected with reason and request resubmission.

### Final state
`Pending Requirements` or `Ready for Approval`.

---

## Flow 6: Assign Section / Schedule
### Objective
Place students without violating capacity and timetable integrity.

### Trigger
Enrollment approved or placement-ready.

### As-Is
1. Select section/class assignment manually.
2. Resolve conflicts iteratively.

### As-Is bottlenecks
1. Conflict feedback may be late.
2. Capacity changes during assignment can invalidate outcomes.

### To-Be
1. Show recommended section/schedule options with real-time constraints.
2. Validate capacity, room-time, teacher-load, and curriculum fit pre-commit.
3. Commit transaction with conflict recheck.

### Decision points
1. Accept recommended placement or manual override (reason required for override).

### Validations
1. Section capacity hard limit.
2. Schedule conflict detection.
3. Teacher load threshold.

### Failure cases and recovery
1. Mid-commit conflict -> rollback and present alternatives.

### Final state
Assigned placement snapshot with audit trail.

---

## Flow 7: Generate Reports
### Objective
Produce repeatable reports quickly and accurately.

### Trigger
Operational/compliance reporting request.

### As-Is
1. Pick report type.
2. Apply filters.
3. Load and export.

### As-Is bottlenecks
1. Repeat filter setup on frequent report runs.
2. Weak run-history visibility.

### To-Be
1. Report catalog with saved presets.
2. Dynamic filter panel by report type.
3. Row-count preview and query progress.
4. Export actions logged in report run history.

### Decision points
1. Ad-hoc vs preset run.
2. Export full result vs filtered subset.

### Validations
1. Required filter set by report type.
2. Date/year bounds.

### Failure cases and recovery
1. Timeout/empty data -> suggest filter refinement without losing current filter state.

### Final state
Rendered report and audited export event.

---

## Flow 8: Archive and Restore Records
### Objective
Manage lifecycle closure and recovery without integrity loss.

### Trigger
Record cleanup, reversal, or correction.

### As-Is
1. Archive/restore action from module.
2. Limited dependency preview.

### As-Is bottlenecks
1. Archive vs soft-delete semantics can be misunderstood.
2. Restore conflicts may surface too late.

### To-Be
1. Pre-action dependency impact panel.
2. Enforce reason code for archive and restore.
3. Restore conflict resolver before commit.

### Decision points
1. Block due to active dependencies or allow policy-based override.

### Validations
1. Referential dependencies.
2. Unique key collisions on restore.

### Failure cases and recovery
1. Restore collision -> staged conflict resolution flow.

### Final state
Archived or restored record with before/after audit payload.

---

## Flow 9: Backup / Restore
### Objective
Protect and recover database safely.

### Trigger
Routine maintenance or incident response.

### As-Is
1. Run backup or restore action.
2. Check logs manually.

### As-Is bottlenecks
1. Inconsistent preflight discipline.
2. Restore impact visibility can be insufficient.

### To-Be
1. Mandatory preflight checks:
   - environment match
   - DB connectivity
   - backup file integrity
   - storage availability
2. Restore impact warning with typed confirmation.
3. Post-restore forced relogin and cache reset.

### Decision points
1. Proceed on warning or abort.

### Validations
1. Connection and tool readiness.
2. Backup artifact validity.

### Failure cases and recovery
1. Corrupt or incompatible artifact -> block restore and surface diagnostics.

### Final state
Backup/restore completed with operation logs and audit event.

---

## Flow 10: Year-End Rollover
### Objective
Execute safe and traceable school-year transition.

### Trigger
End-of-year governance operation.

### As-Is
1. Select source and target year.
2. Execute with options.

### As-Is bottlenecks
1. Preflight and impact review may be too shallow.
2. Recovery confidence depends on manual operator discipline.

### To-Be
1. 4-stage guided wizard:
   - scope
   - preflight
   - impact preview
   - execute and reconcile
2. Typed confirmation on execute.
3. Transactional execution and reconciliation output report.

### Decision points
1. Snapshot source year or not.
2. Close source year or keep open.

### Validations
1. Target year readiness.
2. Mapping completeness.
3. Duplicate prevention.

### Failure cases and recovery
1. Preflight fail -> hard block with remediation checklist.
2. Execute failure -> rollback transaction and produce error pack.

### Final state
Completed rollover with reconciliation metrics and full audit trace.

---

## Cross-Flow Design Rules
1. Single primary action per screen.
2. Destructive actions separated and confirmation-tiered.
3. Inline validation first; summary only for blocking errors.
4. Mandatory reason codes on governed transitions.
5. No modal stacking for long workflows.
6. Consistent list-detail layout across modules.

## Flow KPIs for redesign validation
1. Reduce median completion time for Add Student by at least 30%.
2. Reduce enrollment decision reversals by at least 40%.
3. Reduce requirement-related rework cycle time by at least 35%.
4. Achieve 100% reason-code coverage on governed actions.
5. Achieve 100% preflight execution on backup/restore and rollover.

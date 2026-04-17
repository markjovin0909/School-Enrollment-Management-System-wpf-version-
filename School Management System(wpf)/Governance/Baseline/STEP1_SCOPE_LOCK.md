# Step 1 Scope Lock (Baseline Freeze)

## Purpose
Freeze current workflow scope before redesign and implementation changes, then measure baseline performance and risk metrics against the frozen scope.

## Freeze Date
- 2026-04-08

## Frozen Modules
1. Startup / Environment / Database validation loop
2. Login / Lockout / Account recovery / Session timeout
3. Dashboard
4. Students
5. Teachers
6. Enrollment
7. Reports
8. Operations
   - School Settings
   - School Years
   - Grade Levels
   - Subjects
   - Curriculum
   - Sections
   - Class Offerings
   - Schedules
   - Rooms
   - Time Slots
   - Student Accounts
   - Student Requirements
   - Archive Center
   - Backup / Restore
   - Year-End Rollover

## Frozen High-Impact Workflows
1. Add new student
2. Add new teacher
3. Submit enrollment
4. Review / approve / reject / return enrollment
5. Manage missing requirements
6. Assign section / schedules
7. Generate reports and export CSV
8. Archive and restore records
9. Backup and restore database
10. Execute year-end rollover

## Scope Lock Rules
1. No module removal/addition during baseline period.
2. No status model changes during baseline period.
3. No policy rule changes affecting approvals, archiving, restore, or rollover.
4. No query tuning that materially changes baseline timings while capture is active.
5. Defect fixes are allowed only if they are critical and must be logged in baseline notes.

## Baseline Window
- Recommended: 30 calendar days using current production-like environment.
- Minimum acceptable: 14 days.

## Baseline Outputs Required
1. Baseline metrics JSON snapshot
2. Baseline summary markdown report
3. Raw enrollment status distribution CSV
4. Raw audit action distribution CSV

## Change Control While Frozen
1. Every out-of-scope change request must include:
   - reason
   - risk
   - expected metric impact
   - approval by owner
2. All approved exceptions must be listed in `BaselineReports/*_notes.md`.

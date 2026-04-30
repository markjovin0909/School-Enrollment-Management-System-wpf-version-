# Step 5 Page Implementation Backlog v1

## Metadata
- Version: v1
- Date: 2026-04-30
- Scope: Page-by-page WPF implementation plan aligned to the current shell, windows, and governance specs
- Primary references:
  - `WPF_UI_Redesign_Blueprint.md`
  - `Governance/ScreenSpec/STEP3_SCREEN_BEHAVIOR_SPEC_v1.md`
  - `Governance/Backlog/STEP5_EXECUTION_BACKLOG_v1.md`

## Objective
Turn the approved page structure into an execution-ready backlog that fits the current WPF project instead of introducing a separate UI architecture.

## Delivery Principle
1. Keep the `MainWindow` shell as the primary workspace.
2. Use child windows for create forms, maintenance tools, history, and governed utilities.
3. Standardize behavior before expanding features.
4. Prevent layout collapse under large datasets by enforcing minimum widths, scroll boundaries, and virtualization rules.

## Screen Inventory
### Shell tabs
1. `Dashboard`
2. `Students`
3. `Teachers`
4. `Enrollment`
5. `Reports`
6. `Master Data`
7. `Scheduling`
8. `Accounts & Compliance`
9. `Maintenance`

### Child windows and dialogs
1. `LoginWindow`
2. `StudentCreateWindow`
3. `TeacherCreateWindow`
4. `SchoolSettingsWindow`
5. `SchoolYearsWindow`
6. `GradeLevelsWindow`
7. `SubjectsWindow`
8. `CurriculumWindow`
9. `SectionsWindow`
10. `ClassOfferingsWindow`
11. `SchedulesWindow`
12. `TeacherLoadsWindow`
13. `RoomsWindow`
14. `TimeSlotsWindow`
15. `StudentAccountsWindow`
16. `StudentRequirementsWindow`
17. `ArchiveCenterWindow`
18. `BackupRestoreWindow`
19. `YearEndRolloverWindow`
20. `UserActivityHistoryWindow`
21. `DatabaseConfigurationWindow`
22. `ReasonPromptWindow`
23. `AccountRecoveryWindow`
24. `ChangePasswordWindow`

## Global UI Contract
### Workspace pattern
1. Management pages use `Header -> Filter bar -> Table/List -> Detail panel -> Status bar`.
2. Create pages use `Header -> Validation summary -> Sectioned form -> Footer actions`.
3. Complex workflows use `Header -> Filter ribbon -> Workbench split layout -> Status/validation region`.
4. Navigation hubs use `Header -> Grouped tiles/cards -> Context/help panel`.

### Size and anti-shrink rules
1. Main shell stays `Maximized` with a working minimum not below the current `1180x760`.
2. Table/detail workspaces must keep the detail panel at `MinWidth 380`.
3. Create forms must keep form bodies at `MinWidth 680`.
4. Dense maintenance windows should target `MinWidth 980`.
5. Large grids must use row virtualization and keep horizontal scrolling inside the grid instead of compressing columns.
6. Editor panels scroll vertically; tables own horizontal space.
7. Destructive action groups must never push primary save actions out of view.

### Shared behavior rules
1. One primary action per section.
2. Inline validation on blur plus blocking summary on submit.
3. Governed actions require `ReasonPromptWindow` when policy says so.
4. Empty, loading, error, and success states must exist on every major screen.
5. Search, filter, and sort state should persist per session where already supported by the shell.

## Phase Map
### Phase 0: Standards and shell hardening
Goal: make every later page implementation consistent.

Tasks:
1. Lock shell layout rules in `MainWindow.xaml`.
2. Standardize top bar actions and dashboard entry behavior.
3. Audit shared theme resources in `Themes/` and shared controls in `Controls/`.
4. Define one reusable child-window layout contract across `Views/`.
5. Add or verify virtualization, minimum widths, and editor scroll behavior on all table-heavy screens.

Primary files:
1. `MainWindow.xaml`
2. `Themes/DataGrid.xaml`
3. `Themes/Dialogs.xaml`
4. `Themes/Inputs.xaml`
5. `Controls/PageHeader.xaml`
6. `Controls/SearchToolbar.xaml`
7. `Controls/SectionHeader.xaml`

Exit criteria:
1. No core workspace visually collapses below usable editing width.
2. Shared controls and action hierarchy are consistent in shell and child windows.

### Phase 1: Access and governed utilities
Goal: secure entry and sensitive actions first.

Pages:
1. `LoginWindow`
2. `AccountRecoveryWindow`
3. `ChangePasswordWindow`
4. `DatabaseConfigurationWindow`
5. `ReasonPromptWindow`

Tasks:
1. Finalize login card with inline error, warning, and success messaging.
2. Separate low-emphasis utility actions from primary sign-in flow.
3. Complete recovery flow states: request, verify, reset.
4. Support both standard password change and admin reset mode.
5. Require structured reason capture for archive, restore, drop, cancel, restore-run, and rollover actions.

Dependencies:
1. `AuthService`
2. `AccountSecurityService`
3. `PasswordPolicyService`
4. `LoginAttemptService`
5. `PermissionBoundaryService`

Exit criteria:
1. Admin access and recovery flows match the governance spec.
2. Sensitive actions cannot proceed without the required confirmation payload.

### Phase 2: Shell hubs and dashboard
Goal: make navigation predictable before deep CRUD work.

Pages:
1. `Dashboard`
2. `Master Data`
3. `Scheduling`
4. `Accounts & Compliance`
5. `Maintenance`

Tasks:
1. Build dashboard KPI cards from current operational metrics services.
2. Add quick-launch groups that match the requested page taxonomy.
3. Add recent activity grid with clear empty/loading/failure states.
4. Keep the hub pages as launch surfaces only; do not overload them with form editing.
5. Ensure tile group naming matches the requested terminology exactly.

Primary files:
1. `MainWindow.xaml`
2. `MainWindow.xaml.cs`
3. `Services/OperationalMetricsDashboardService.cs`
4. `Services/AuditLogService.cs`

Exit criteria:
1. Users can reach every operational area in at most two clicks from the shell.
2. Dashboard shows stable summary signals without modal noise.

### Phase 3: Core identity management
Goal: establish the standard list-detail workflow on the highest-volume modules.

Pages:
1. `Students`
2. `StudentCreateWindow`
3. `Teachers`
4. `TeacherCreateWindow`

Tasks:
1. Expand student table columns to match the approved structure where data exists.
2. Re-group the student editor into identity, contact, guardian, and academic sections.
3. Keep create flows separate from edit flows.
4. Surface account state, archive state, and history actions without mixing them into the primary save path.
5. Mirror the student interaction model on the teacher side for consistency.
6. Ensure `Reset Account` and `Reset Password` are visually separated from save actions.

Primary files:
1. `MainWindow.Students.cs`
2. `MainWindow.Teachers.cs`
3. `Views/StudentCreateWindow.xaml`
4. `Views/TeacherCreateWindow.xaml`
5. `Services/StudentService.cs`
6. `Services/TeacherService.cs`

Exit criteria:
1. Students and Teachers behave as the canonical management-page pattern.
2. No create/edit workflow requires modal stacking beyond the dedicated create window.

### Phase 4: Enrollment workbench
Goal: make enrollment a controlled decision workspace instead of simple CRUD.

Pages:
1. `Enrollment`
2. `StudentRequirementsWindow`
3. `ReasonPromptWindow` integration

Tasks:
1. Finalize top filter bar: school year, grade level, curriculum, status, student search.
2. Keep the left table focused on queue browsing and student selection.
3. Use the right side for selected student summary, requirement checklist, offerings, and governed actions.
4. Add requirement completeness indicators and missing-document warnings.
5. Separate primary, secondary, and destructive enrollment actions.
6. Revalidate conflicts at commit time for seat availability, duplicate active enrollment, and stale data.

Primary files:
1. `MainWindow.Enrollment.cs`
2. `MainWindow.xaml`
3. `Services/EnrollmentService.cs`
4. `Services/EnrollmentStateMachineService.cs`
5. `Services/RequirementChecklistService.cs`
6. `Services/StudentRequirementService.cs`

Exit criteria:
1. Enrollment decisions can be made from one workbench without jumping between unrelated windows.
2. Policy-required transitions are auditable and gated.

### Phase 5: Academic master data
Goal: complete the underlying academic setup screens in a uniform way.

Pages:
1. `SchoolSettingsWindow`
2. `SchoolYearsWindow`
3. `GradeLevelsWindow`
4. `SubjectsWindow`
5. `CurriculumWindow`
6. `SectionsWindow`

Tasks:
1. Align each screen to the approved field lists and action sets.
2. Standardize the left-table right-detail pattern across the data editors.
3. Keep curriculum subject mapping as a dedicated lower panel, not crammed into a single form band.
4. Surface archive state consistently on entities that support soft delete.
5. Enforce lookup dependencies between school years, grades, curricula, and sections.

Primary files:
1. `Views/SchoolSettingsWindow.xaml`
2. `Views/SchoolYearsWindow.xaml`
3. `Views/GradeLevelsWindow.xaml`
4. `Views/SubjectsWindow.xaml`
5. `Views/CurriculumWindow.xaml`
6. `Views/SectionsWindow.xaml`

Exit criteria:
1. Academic setup data is complete enough to drive enrollment and scheduling.
2. Every screen follows one recognizable editing pattern.

### Phase 6: Scheduling stack
Goal: finish operational planning screens after master data is stable.

Pages:
1. `ClassOfferingsWindow`
2. `SchedulesWindow`
3. `TeacherLoadsWindow`
4. `RoomsWindow`
5. `TimeSlotsWindow`

Tasks:
1. Make `Class Offerings` generation and finalize flows explicit.
2. Add schedule conflict warnings for teacher, room, section, and overlap.
3. Keep teacher load review mostly read-optimized with detail drilldown.
4. Keep rooms and time slots lightweight but consistent with the management-page contract.
5. Add export actions only where they support a real operational need.

Primary files:
1. `Views/ClassOfferingsWindow.xaml`
2. `Views/SchedulesWindow.xaml`
3. `Views/TeacherLoadsWindow.xaml`
4. `Views/RoomsWindow.xaml`
5. `Views/TimeSlotsWindow.xaml`
6. `Services/ClassOfferingService.cs`
7. `Services/ClassScheduleService.cs`
8. `Services/TimeSlotService.cs`

Exit criteria:
1. Scheduling screens prevent silent conflicts.
2. Capacity and timing decisions are visible before save, not after failure.

### Phase 7: Accounts, compliance, and archive
Goal: close the loop on account inspection, documents, and soft-delete recovery.

Pages:
1. `StudentAccountsWindow`
2. `StudentRequirementsWindow`
3. `ArchiveCenterWindow`
4. `UserActivityHistoryWindow`

Tasks:
1. Make student accounts inspectable without editing unrelated student data.
2. Present requirement tracking per student with clear status and notes.
3. Add archive recovery context with deleted-by, deleted-date, and restore state.
4. Keep activity history filterable by date, user, module, action type, and entity type.

Primary files:
1. `Views/StudentAccountsWindow.xaml`
2. `Views/StudentRequirementsWindow.xaml`
3. `Views/ArchiveCenterWindow.xaml`
4. `Views/UserActivityHistoryWindow.xaml`
5. `Services/ArchiveRecordService.cs`
6. `Services/AuditLogService.cs`

Exit criteria:
1. Staff can audit and recover records without leaving the governance model.
2. Account and requirement monitoring are operationally separate from core record editing.

### Phase 8: Reporting and maintenance execution
Goal: complete read-heavy and high-risk operational flows last.

Pages:
1. `Reports`
2. `BackupRestoreWindow`
3. `YearEndRolloverWindow`

Tasks:
1. Make report selection the first-class interaction on the Reports tab.
2. Keep report filters dynamic and tied to report type.
3. Support presets, history, and export without overloading the initial state.
4. Harden backup/restore with explicit status, history, and preflight feedback.
5. Keep year-end rollover as a preview-first, reason-gated, dedicated process screen.

Primary files:
1. `MainWindow.Reports.cs`
2. `Views/BackupRestoreWindow.xaml`
3. `Views/YearEndRolloverWindow.xaml`
4. `Services/CsvExportService.cs`
5. `Services/PdfReportService.cs`
6. `Services/BackupRestoreService.cs`
7. `Services/PreflightPipelineService.cs`

Exit criteria:
1. Reporting is fast and readable for large result sets.
2. Backup, restore, and rollover cannot execute without visible readiness feedback.

## Page-by-Page Checklist
### Shell pages
1. `Dashboard`
   - KPI summary cards
   - recent activity
   - quick-launch groups
   - refresh state
2. `Students`
   - searchable table
   - full detail editor
   - account actions
   - archive/history integration
3. `Teachers`
   - searchable table
   - employment-focused detail editor
   - password/history actions
4. `Enrollment`
   - queue table
   - selected student summary
   - offerings table
   - requirements checklist
   - governed action rail
5. `Reports`
   - report selector
   - dynamic filters
   - result grid
   - export/preset/history
6. `Master Data`
   - launch cards only
7. `Scheduling`
   - launch cards only
8. `Accounts & Compliance`
   - launch cards only
9. `Maintenance`
   - launch cards only

### Child windows
1. `LoginWindow`
   - username/email
   - password
   - remember me
   - recovery
   - inline status messages
2. `StudentCreateWindow`
   - basic information
   - address/contact
   - guardian
   - academic preference
3. `TeacherCreateWindow`
   - account
   - identity
   - employment
   - assignment
4. `SchoolSettingsWindow`
   - profile
   - principal/admin
   - numbering
   - defaults
   - system
5. `SchoolYearsWindow`
   - table
   - detail panel
   - archive/restore
6. `GradeLevelsWindow`
   - table
   - detail panel
7. `SubjectsWindow`
   - table
   - detail panel
8. `CurriculumWindow`
   - curriculum list
   - editor
   - subject mapping
9. `SectionsWindow`
   - table
   - detail panel
10. `ClassOfferingsWindow`
   - filter bar
   - offerings table
   - detail panel
11. `SchedulesWindow`
   - schedule table
   - editor
   - conflict warnings
12. `TeacherLoadsWindow`
   - filter bar
   - teacher load table
   - selected teacher detail
13. `RoomsWindow`
   - table
   - detail panel
14. `TimeSlotsWindow`
   - table
   - detail panel
15. `StudentAccountsWindow`
   - accounts table
   - detail panel
   - reset/history actions
16. `StudentRequirementsWindow`
   - student selector
   - checklist table
   - requirement detail
17. `ArchiveCenterWindow`
   - archive table
   - detail panel
   - restore action
18. `BackupRestoreWindow`
   - backup controls
   - restore controls
   - history
   - status panel
19. `YearEndRolloverWindow`
   - current context
   - options
   - preview table
   - execute/export actions
20. `UserActivityHistoryWindow`
   - audit grid
   - filters
21. `DatabaseConfigurationWindow`
   - server
   - database name
   - auth type
   - credentials
   - test/save actions
22. `ReasonPromptWindow`
   - reason code
   - explanation
   - confirm/cancel
23. `AccountRecoveryWindow`
   - request
   - verify
   - reset
24. `ChangePasswordWindow`
   - normal mode
   - admin reset mode

## Suggested Sprint Order
1. Sprint 1
   - Phase 0
   - Phase 1
2. Sprint 2
   - Phase 2
   - Students and Teachers from Phase 3
3. Sprint 3
   - Finish Phase 3
   - Phase 4
4. Sprint 4
   - Phase 5
5. Sprint 5
   - Phase 6
6. Sprint 6
   - Phase 7
   - Phase 8

## File-by-File Execution Order
1. `Themes/`
2. `Controls/`
3. `MainWindow.xaml`
4. `MainWindow.xaml.cs`
5. `MainWindow.Students.cs`
6. `MainWindow.Teachers.cs`
7. `MainWindow.Enrollment.cs`
8. `MainWindow.Reports.cs`
9. `Views/LoginWindow.xaml`
10. `Views/AccountRecoveryWindow.xaml`
11. `Views/ChangePasswordWindow.xaml`
12. `Views/DatabaseConfigurationWindow.xaml`
13. `Views/ReasonPromptWindow.xaml`
14. `Views/StudentCreateWindow.xaml`
15. `Views/TeacherCreateWindow.xaml`
16. Remaining `Views/*Window.xaml` files by phase order
17. Corresponding `Services/` files

## Definition of Done
1. Layout matches the approved page role: shell, management page, form page, workbench, or utility dialog.
2. Minimum widths and scrolling behavior prevent unusable compression.
3. Required validations and governed prompts are enforced.
4. Empty, loading, error, and success states exist.
5. Audit behavior is preserved for governed actions.
6. Manual verification covers small dataset and large dataset behavior.

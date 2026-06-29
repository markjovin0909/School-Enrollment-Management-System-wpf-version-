# Modal-Based CRUD Refactor Plan

## Status
- Implementation status: `Structurally complete in code`
- Build status: `dotnet build "School Management System(wpf).sln"` passes with `0` errors and `0` warnings as of `2026-06-18`
- Remaining work: manual WPF smoke testing and optional UX polish outside the original refactor scope

## Goal
Make every scoped entity's CRUD flow run through modal dialogs consistently, reusing each window's existing editor markup and validation rather than maintaining separate inline edit paths.

## Current State
- `Create`: modal across the scoped entities
- `Edit/Update`: modal across the scoped entities in this plan
- `Delete / Archive / Finalize`: core confirmations in this refactor now route through `AppFeedbackService.Confirm(...)`

The inline edit/update paths targeted by this plan have been removed or bypassed in favor of explicit modal edit actions.

## Design
Reuse each existing editor surface where practical. The core refactor pattern is:

```csharp
enum EditorMode { ListEmbedded, Create, Edit }
```

Applied behavior:
1. Add an edit constructor or dedicated edit modal entry point.
2. Configure create and edit modes explicitly.
3. Route the primary action by mode.
4. Open edit from list affordances such as double-click or explicit Edit buttons.
5. Leave list mode read-oriented instead of mutating records inline.

## Scope

### A. Config/list windows
Entities:
- `GradeLevels`
- `Rooms`
- `Subjects`
- `Sections`
- `SchoolYears`
- `TimeSlots`
- `Schedules`
- `Curriculum`
- `StudentRequirements`

Status:
- `Done` for all listed config/list windows
- `Done` for Curriculum subject mapping modal extraction

### B. Students
Target:
- Generalize `StudentCreateWindow` for create and edit
- Replace inline student edit in `MainWindow.Students.cs`
- Route `MainWindow.StudentDetails.cs` edit through the same modal

Status:
- `Done`

### C. Teachers
Target:
- Generalize `TeacherCreateWindow` for create and edit
- Replace inline teacher edit in `MainWindow.Teachers.cs`
- Route `MainWindow.TeacherDetails.cs` edit through the same modal

Status:
- `Done`

### D. Irregular entities
Targets:
- `ClassOfferings`: add dedicated edit modal for teacher / room / status
- `Enrollment`: keep modal wizard and standardize remaining confirm / reason paths in refactor scope

Status:
- `ClassOfferings`: `Done`
- `Enrollment` modal wizard retained: `Done`
- `Enrollment` core cancel / drop confirmation cleanup in this plan scope: `Done`

## Shared Cleanup
Targets:
- Standardize confirmations through `AppFeedbackService.Confirm(...)`
- Apply shared dialog section/action styles to modal CRUD surfaces
- Avoid root-window `StaticResource` style dependencies that fail before local window resources are loaded
- Preserve existing audit logging through modal save / delete / archive paths

Status:
- `AppFeedbackService.Confirm(...)`: `Done`
- Shared confirmation adoption for CRUD flows covered by this refactor: `Done`
- Shared dialog section/action styling on modal CRUD surfaces: `Done`
- Root-window `DialogWindow` style adoption: `Rolled back where it caused WPF parse-time resource failures`
- Audit logging retained in modal paths: `Done`

## Execution Result
1. Add `AppFeedbackService.Confirm(...)` helper: `Done`
2. GradeLevels template: `Done`
3. Roll pattern across remaining config windows: `Done`
4. Students modal edit + detail routing: `Done`
5. Teachers modal edit + detail routing: `Done`
6. ClassOfferings edit modal: `Done`
7. Enrollment status-change cleanup in plan scope: `Done`
8. Build and fix compile issues: `Done`
9. Manual smoke test each entity: `Pending`

## Notes
- The intended UX shift is now in place: row selection no longer acts as an inline editing surface for the refactored entities.
- The repo builds successfully after the refactor.
- I cannot fully verify WPF interaction behavior from here, so interactive smoke testing remains necessary.

## Remaining Verification
- Manual smoke test `Students`: create, edit, archive, restore, reset account, detail-page edit routing
- Manual smoke test `Teachers`: create, edit, archive, restore, reset password, detail-page edit routing
- Manual smoke test `StudentRequirements`: list view, create modal, edit modal, delete confirmation
- Manual smoke test `Curriculum`: curriculum edit mode plus mapping add / edit / remove modal flow
- Manual smoke test `ClassOfferings`: generate, edit modal, finalize confirmation, delete confirmation
- Manual smoke test `Enrollment`: create wizard, cancel, drop, reason prompt flows

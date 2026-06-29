# Hosted Operations Smoke Test Checklist

## Purpose
Validate the hosted Operations modules after the dialog-owner fixes. Focus on create, edit, delete/archive, and secondary modal flows opened from embedded module pages inside `MainWindow`.

## Preconditions
- Build is current and launches successfully.
- Use a test database or data set that can tolerate CRUD verification.
- Sign in with an account that can access:
  - Master Data
  - Scheduling
  - Accounts & Compliance
  - Maintenance

## Pass Criteria
- No immediate crash or hidden-window error when opening any modal dialog.
- Each modal opens in front of the visible main shell.
- Successful save closes the modal and refreshes the hosting list.
- Validation messages remain visible and actionable.
- Delete/archive/restore confirmations appear normally and complete the expected action.

## Master Data

### School Settings
1. Open `Operations > Master Data > School Settings`.
2. Change one non-critical field.
3. Save.
4. Confirm success message appears and the screen remains usable.

### School Years
1. Open `Operations > Master Data > School Years`.
2. Click `New`.
3. Confirm the create modal opens.
4. Create a test school year.
5. Re-open the row using edit.
6. Change one field and save.
7. Archive or restore one record if allowed.

### Grade Levels
1. Open `Operations > Master Data > Grade Levels`.
2. Create a test grade level.
3. Edit the same row.
4. Delete it if not referenced.
5. If referenced, confirm the validation warning is shown instead of a crash.

### Subjects
1. Open `Operations > Master Data > Subjects`.
2. Create a test subject.
3. Edit the same subject.
4. Archive the subject.
5. Confirm list refresh works after each modal close.

### Curriculum
1. Open `Operations > Master Data > Curriculum`.
2. Create a test curriculum.
3. Edit the curriculum.
4. Add a curriculum mapping.
5. Edit the mapping.
6. Remove the mapping.
7. Delete the curriculum if safe.

### Sections
1. Open `Operations > Master Data > Sections`.
2. Create a test section.
3. Edit the section.
4. Archive or restore it.
5. Confirm adviser/school-year/grade-level selections persist correctly.

## Scheduling

### Class Offerings
1. Open `Operations > Scheduling > Class Offerings`.
2. Select a row and open edit.
3. Save at least one field change.
4. Finalize a draft offering if safe.
5. Delete a test offering if safe.

### Schedules
1. Open `Operations > Scheduling > Schedules`.
2. Create a test schedule.
3. Edit the same schedule.
4. Delete it.
5. Confirm warnings and conflicts display without breaking the modal.

### Rooms
1. Open `Operations > Scheduling > Rooms`.
2. Create a test room.
3. Edit the room.
4. Delete it if not referenced.

### Time Slots
1. Open `Operations > Scheduling > Time Slots`.
2. Create a test time slot.
3. Edit the time slot.
4. Delete it.

## Accounts & Compliance

### Student Requirements
1. Open `Operations > Accounts & Compliance > Student Requirements`.
2. Select a student.
3. Create a requirement entry.
4. Edit that requirement.
5. Delete it.

### Student Accounts
1. Open `Operations > Accounts & Compliance > Student Accounts`.
2. Select a row.
3. Open account history.
4. Confirm the history dialog opens in front of the main shell.
5. Run one safe non-destructive action such as refresh or sync if appropriate.

### Archive Center
1. Open `Operations > Accounts & Compliance > Archive Center`.
2. Select an archived test record.
3. Trigger restore preview.
4. Confirm preview and confirmation dialogs appear normally.
5. Complete restore only if safe for the test data set.

## Maintenance

### Backup / Restore
1. Open `Operations > Maintenance > Backup / Restore`.
2. Open backup-folder picker.
3. Open restore-file picker.
4. Open MySQL-bin-folder picker.
5. Confirm all pickers open from the hosted module without disappearing behind the main shell.
6. Do not run restore against live data unless explicitly intended.

### Year-End Rollover
1. Open `Operations > Maintenance > Year-End Rollover`.
2. Change source/target selections.
3. Run preview only.
4. Confirm warnings and confirmations appear correctly.
5. Execute only in a disposable test data set.

## Defect Logging Template
Record each failure with:
- Module name
- Action being performed
- Expected result
- Actual result
- Exact error message text
- Whether the modal appeared at all
- Whether the window appeared behind the main shell

## Notes
- The key regression this checklist targets is dialogs launched from embedded hosted modules using a hidden owner window.
- If any modal fails, note the exact module and button path because the fix pattern is now centralized and should be fast to extend.

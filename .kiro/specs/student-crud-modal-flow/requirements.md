# Requirements Document

## Introduction

This feature revises the Student CRUD operation flow in the WPF School Management System. Instead of navigating directly to separate windows, the admin will interact with a modal-based workflow triggered from the dashboard's "Students" quick action. The modal provides a search bar and an "Add Student" button. Selecting a student or adding a new one leads to a unified Student Details window that consolidates personal info, requirements, grades, and subjects, with edit and delete capabilities.

## Glossary

- **Student_Modal**: The modal dialog that appears when the admin clicks the "Students" quick action, containing a search bar and an "Add Student" button.
- **Search_Bar**: A text input within the Student_Modal that allows the admin to filter and find students by name, LRN, or student number.
- **Add_Student_Form**: The form displayed within the Student_Modal for creating a new student record.
- **Student_Details_Window**: A dedicated window that displays all information about a selected student, including personal info, requirements, grades, and enrolled subjects.
- **Admin**: The authenticated user operating the School Management System with administrative privileges.
- **Student_Record**: The data entity representing a student, including personal information, guardian details, academic preferences, and status.
- **LRN**: Learner Reference Number, a unique identifier assigned to each student.
- **Student_Number**: A system-generated unique number assigned to each student upon creation.

## Requirements

### Requirement 1: Student Modal Launch

**User Story:** As an Admin, I want to open a Student Modal from the dashboard quick action, so that I can quickly access student search and creation functionality without navigating away from the main interface.

#### Acceptance Criteria

1. WHEN the Admin clicks the "Students" quick action button, THE Student_Modal SHALL appear as a modal dialog overlaying the current window within 1 second.
2. THE Student_Modal SHALL contain a Search_Bar and an "Add Student" button upon opening, with the Search_Bar empty and the full student list displayed.
3. WHILE the Student_Modal is open, THE System SHALL prevent interaction with the underlying window.
4. WHEN the Admin closes the Student_Modal by clicking the modal's close button or pressing the Escape key, THE System SHALL dismiss the Student_Modal and return focus to the main window without creating or modifying any Student_Record.
5. IF the Admin clicks outside the Student_Modal content area, THEN THE System SHALL dismiss the Student_Modal and return focus to the main window without creating or modifying any Student_Record.

### Requirement 2: Student Search

**User Story:** As an Admin, I want to search for students within the modal, so that I can quickly find and select a specific student to view their details.

#### Acceptance Criteria

1. WHEN the Admin types text into the Search_Bar, THE Student_Modal SHALL filter the displayed student list to show only students whose full name, LRN, or student number contains the search text as a case-insensitive substring match, updating results within 300 milliseconds of the last keystroke.
2. THE Student_Modal SHALL display matching students in a scrollable list format showing at minimum the student's full name, LRN, and student number.
3. WHEN no students match the search text, THE Student_Modal SHALL display a message indicating no results were found and hide the student list.
4. WHEN the Admin clears the Search_Bar, THE Student_Modal SHALL display the full list of students.
5. THE Search_Bar SHALL perform filtering without requiring the Admin to press a separate search button.
6. WHEN the Student_Modal first opens, THE Student_Modal SHALL display the full list of students in the same list format used for search results.

### Requirement 3: Student Selection and Navigation

**User Story:** As an Admin, I want to select a student from the search results, so that I can view their complete details in a dedicated window.

#### Acceptance Criteria

1. WHEN the Admin double-clicks a student from the search results list, THE System SHALL close the Student_Modal and open the Student_Details_Window for the selected student within 2 seconds.
2. THE Student_Details_Window SHALL display the selected student's personal information including: full name, LRN, student number, birthdate, age, sex, address, contact number, guardian name, guardian contact, previous school, preferred grade level, preferred curriculum, and status.
3. THE Student_Details_Window SHALL display the selected student's requirements checklist showing each requirement's name, submission status (submitted or not submitted), submission date if submitted, and notes.
4. THE Student_Details_Window SHALL display the selected student's grades organized by class offering and grading period, showing for each entry: written works, performance tasks, quarterly assessment, and quarter grade.
5. THE Student_Details_Window SHALL display the selected student's enrolled subjects and class assignments, showing for each entry: the class offering name, associated enrollment, and enrollment status.
6. IF the System fails to load the selected student's data, THEN THE System SHALL display an error message indicating the data could not be retrieved and keep the Admin on the Student_Modal.
7. THE Student_Details_Window SHALL provide a navigation control that allows the Admin to close the window and return focus to the main dashboard.

### Requirement 4: Add Student Flow

**User Story:** As an Admin, I want to add a new student from the modal, so that I can register new students and immediately view their details.

#### Acceptance Criteria

1. WHEN the Admin clicks the "Add Student" button in the Student_Modal, THE System SHALL display the Add_Student_Form within the modal.
2. THE Add_Student_Form SHALL collect the following required fields: LRN (maximum 12 characters), first name (maximum 100 characters), and last name (maximum 100 characters).
3. THE Add_Student_Form SHALL collect the following optional fields: middle name, profile image URL, birthdate, sex, address, contact number, guardian name, guardian contact, previous school, preferred grade level, preferred curriculum, and status.
4. WHEN the Admin submits the Add_Student_Form with all required fields populated and no uniqueness violations, THE System SHALL generate a Student_Number, create the Student_Record, close the Student_Modal, and open the Student_Details_Window for the newly created student.
5. IF the Admin submits the Add_Student_Form with a duplicate LRN (case-insensitive, ignoring leading and trailing whitespace), THEN THE System SHALL display a validation error indicating the LRN already exists, preserve all entered form data, and prevent creation.
6. IF the Admin submits the Add_Student_Form with a duplicate combination of first name, middle name, last name, and birthdate (case-insensitive, ignoring leading and trailing whitespace, evaluated only when birthdate is provided), THEN THE System SHALL display a validation error indicating a student with the same identity already exists, preserve all entered form data, and prevent creation.
7. IF the Admin submits the Add_Student_Form with missing required fields, THEN THE System SHALL visually distinguish each invalid field from valid fields, display a validation summary listing all errors, preserve all entered form data, and prevent creation.
8. WHEN the Admin cancels the Add_Student_Form, THE System SHALL return to the Student_Modal search view without creating a student.

### Requirement 5: Edit Student

**User Story:** As an Admin, I want to edit a student's details from the Student Details window, so that I can update their information when changes occur.

#### Acceptance Criteria

1. THE Student_Details_Window SHALL display an "Edit" button while in view mode.
2. WHEN the Admin clicks the "Edit" button, THE Student_Details_Window SHALL switch to an editable mode where all student fields become modifiable except for the system-generated Student_Number, and SHALL display "Save" and "Cancel" buttons in place of the "Edit" button.
3. WHEN the Admin clicks the "Save" button with valid values in all required fields (LRN, first name, and last name), THE System SHALL update the Student_Record and return the Student_Details_Window to view mode with the updated information.
4. IF the Admin clicks the "Save" button with any required field (LRN, first name, or last name) empty or blank, THEN THE System SHALL highlight the invalid fields, display a validation summary, and remain in edit mode with the entered data preserved.
5. IF the Admin saves edited data that violates uniqueness constraints on LRN or name-birthdate combination, THEN THE System SHALL display a validation error indicating which constraint was violated and remain in edit mode with the entered data preserved.
6. WHEN the Admin clicks the "Cancel" button while in edit mode, THE Student_Details_Window SHALL discard unsaved changes and return to view mode displaying the last saved data.

### Requirement 6: Delete Student

**User Story:** As an Admin, I want to delete a student from the Student Details window, so that I can remove records that are no longer needed.

#### Acceptance Criteria

1. THE Student_Details_Window SHALL display a "Delete" button.
2. WHEN the Admin clicks the "Delete" button, THE System SHALL display a confirmation dialog asking the Admin to confirm the deletion.
3. IF the Student_Record has associated dependent records (enrollments, grades, class assignments, attendance records, assessment scores, or student requirements), THEN THE System SHALL display a warning within the confirmation dialog that lists the types and counts of dependent records that will also be removed, and require the Admin to explicitly confirm or cancel the deletion.
4. WHEN the Admin confirms the deletion, THE System SHALL permanently remove the Student_Record and all associated dependent records, close the Student_Details_Window, and return focus to the main window.
5. WHEN the Admin cancels the deletion confirmation, THE System SHALL dismiss the confirmation dialog and remain on the Student_Details_Window with no changes to the Student_Record.
6. IF the deletion operation fails due to a system error, THEN THE System SHALL display an error message indicating the deletion could not be completed, retain the Student_Record unchanged, and remain on the Student_Details_Window.

### Requirement 7: Audit Trail

**User Story:** As an Admin, I want all student CRUD operations to be logged, so that there is a traceable history of changes for accountability.

#### Acceptance Criteria

1. WHEN a Student_Record is created, THE System SHALL log an audit entry containing the performing user's identifier, the operation type "CREATE", the entity name "students", the new record's entity identifier, a payload with the created field values, and a UTC timestamp.
2. WHEN a Student_Record is updated, THE System SHALL log an audit entry containing the performing user's identifier, the operation type "UPDATE", the entity name "students", the record's entity identifier, a payload with both the previous field values and the new field values, and a UTC timestamp.
3. WHEN a Student_Record is deleted, THE System SHALL log an audit entry containing the performing user's identifier, the operation type "DELETE", the entity name "students", the record's entity identifier, a payload with the deleted record's field values, and a UTC timestamp.
4. IF the System fails to persist an audit entry during a Student_Record create, update, or delete operation, THEN THE System SHALL prevent the CRUD operation from completing and display an error message indicating the operation could not be logged.
5. THE System SHALL record each audit entry within the same transaction as the corresponding Student_Record operation so that no CRUD change exists without a matching audit log entry.

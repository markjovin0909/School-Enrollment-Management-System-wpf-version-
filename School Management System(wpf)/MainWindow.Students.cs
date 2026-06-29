using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.Views;

namespace School_Management_System
{
    public partial class MainWindow
    {
        private void InitializeStudentsTab()
        {
            cboStudentStatus.ItemsSource = Enum.GetValues(typeof(UserStatus));
            cboStudentSex.ItemsSource = Enum.GetValues(typeof(Sex));
            txtStudentSearch.Text = GetSessionState("students.search");
            cboStudentTableStatus.ItemsSource = new[] { "All Statuses", "Active", "Inactive", "Locked", "Archived" };
            cboStudentTableStatus.SelectedIndex = 0;

            btnStudentsNew.Click += (_, _) => OpenCreateStudentDialog();
            btnStudentsRefresh.Click += (_, _) => LoadStudents();
            btnStudentAdd.Click += (_, _) => OpenCreateStudentDialog();
            btnStudentSave.Click += (_, _) => OpenEditStudentDialog();
            btnStudentArchiveRestore.Click += (_, _) => ArchiveOrRestoreStudent();
            btnStudentResetAccount.Click += (_, _) => ResetStudentAccount();
            btnStudentHistory.Click += (_, _) => OpenStudentHistory();
            btnStudentRequirements.Click += (_, _) => OpenStudentRequirements();
            btnStudentAccounts.Click += (_, _) => OpenStudentAccounts();
            btnStudentAccountHistory.Click += (_, _) => OpenStudentAccountHistory();
            btnStudentClear.Click += (_, _) => ClearStudentEditor();
            btnStudentSave.Content = "Edit Selected";

            txtStudentSearch.TextChanged += (_, _) =>
            {
                if (_suppressStudentEvents) return;
                SetSessionState("students.search", txtStudentSearch.Text);
                LoadStudents();
            };
            cboStudentTableGrade.SelectionChanged += (_, _) =>
            {
                if (_suppressStudentEvents) return;
                LoadStudents();
            };
            cboStudentTableStatus.SelectionChanged += (_, _) =>
            {
                if (_suppressStudentEvents) return;
                LoadStudents();
            };
            gridStudents.AutoGeneratingColumn += GridStudents_AutoGeneratingColumn;
            gridStudents.SelectionChanged += GridStudents_SelectionChanged;
            WireGridSortPersistence(gridStudents, "students");

            SetStudentViewerMode();
            LoadStudentPreferenceLookups();
            ClearStudentEditor();
            LoadStudents();
        }

        private void OpenCreateStudentDialog()
        {
            try
            {
                var dialog = new StudentCreateWindow { Owner = this };
                if (dialog.ShowDialog() == true && dialog.SavedStudentId.HasValue)
                {
                    LoadStudentPreferenceLookups();
                    LoadStudents(dialog.SavedStudentId.Value);
                }
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError("Open create student dialog failed.", ex, "Students", this);
            }
        }

        private void OpenEditStudentDialog()
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student first.", "Edit Student", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new StudentCreateWindow(_selectedStudentId.Value) { Owner = this };
                if (dialog.ShowDialog() == true && dialog.SavedStudentId.HasValue)
                {
                    LoadStudentPreferenceLookups();
                    LoadStudents(dialog.SavedStudentId.Value);
                }
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError("Open edit student dialog failed.", ex, "Edit Student", this);
            }
        }

        private void LoadStudentPreferenceLookups()
        {
            var defaultGradeLevelIds = _schoolSettingService.GetDefaultGradeLevelIds().ToHashSet();
            _gradeLevels = _gradeLevelService.GetAll().OrderBy(g => g.Name).ToList();
            var gradeItems = _gradeLevels
                .OrderBy(g => defaultGradeLevelIds.Contains(g.Id) ? 0 : 1)
                .ThenBy(g => g.Name)
                .Select(g => new LookupChoice(
                    g.Id,
                    string.IsNullOrWhiteSpace(g.Code)
                        ? g.Name
                        : string.IsNullOrWhiteSpace(g.Name)
                            ? g.Code
                            : $"{g.Code} - {g.Name}"))
                .ToList();
            gradeItems.Insert(0, new LookupChoice(0, "(Not set)"));

            cboStudentPreferredGrade.DisplayMemberPath = nameof(LookupChoice.Label);
            cboStudentPreferredGrade.SelectedValuePath = nameof(LookupChoice.Id);
            cboStudentPreferredGrade.ItemsSource = gradeItems;

            if (defaultGradeLevelIds.Count > 0)
            {
                cboStudentPreferredGrade.SelectedValue = gradeItems.FirstOrDefault(x => defaultGradeLevelIds.Contains(x.Id))?.Id ?? 0L;
            }
            else
            {
                cboStudentPreferredGrade.SelectedValue = 0L;
            }

            var tableGradeItems = new List<LookupChoice> { new(0, "All Grades") };
            tableGradeItems.AddRange(gradeItems.Where(x => x.Id > 0));
            cboStudentTableGrade.DisplayMemberPath = nameof(LookupChoice.Label);
            cboStudentTableGrade.SelectedValuePath = nameof(LookupChoice.Id);
            cboStudentTableGrade.ItemsSource = tableGradeItems;
            if (cboStudentTableGrade.SelectedIndex < 0)
            {
                cboStudentTableGrade.SelectedIndex = 0;
            }

            var curriculumItems = _curriculumService.GetAll()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new LookupChoice(c.Id, c.Name))
                .ToList();
            curriculumItems.Insert(0, new LookupChoice(0, "(Not set)"));

            cboStudentPreferredCurriculum.DisplayMemberPath = nameof(LookupChoice.Label);
            cboStudentPreferredCurriculum.SelectedValuePath = nameof(LookupChoice.Id);
            cboStudentPreferredCurriculum.ItemsSource = curriculumItems;
            cboStudentPreferredCurriculum.SelectedValue = 0L;
        }

        private void LoadStudents(long? preferredId = null)
        {
            try
            {
                _students = _studentService.GetAll().ToList();
                var usersById = _userService.GetAll().ToDictionary(u => u.Id, u => u);
                // Cache enrollments for reuse in selection-changed handler — avoids a second GetAll() per row click
                _cachedEnrollments = _enrollmentService.GetAll().ToList();
                var enrollmentsByStudentId = _cachedEnrollments
                    .GroupBy(x => x.StudentId)
                    .ToDictionary(
                        x => x.Key,
                        x => x.OrderByDescending(e => e.UpdatedAt).ThenByDescending(e => e.CreatedAt).First());
                // Build curriculum lookup once — avoids a DB round-trip per student row
                var curriculumById = _curriculumService.GetAll()
                    .ToDictionary(c => c.Id, c => c.Name);

                _studentsTable = new DataTable();
                _studentsTable.Columns.Add("Id", typeof(long));
                _studentsTable.Columns.Add("StudentNo");
                _studentsTable.Columns.Add("LRN");
                _studentsTable.Columns.Add("FullName");
                _studentsTable.Columns.Add("GradeLevel");
                _studentsTable.Columns.Add("Curriculum");
                _studentsTable.Columns.Add("Sex");
                _studentsTable.Columns.Add("ContactNo");
                _studentsTable.Columns.Add("Guardian");
                _studentsTable.Columns.Add("EnrollmentStatus");
                _studentsTable.Columns.Add("AccountStatus");
                _studentsTable.Columns.Add("RecordStatus");

                var term = (txtStudentSearch.Text ?? string.Empty).Trim();
                var selectedGradeId = cboStudentTableGrade.SelectedValue is long gradeId && gradeId > 0 ? gradeId : 0L;
                var statusFilter = (cboStudentTableStatus.SelectedItem as string ?? "All Statuses").Trim();
                foreach (var s in _students)
                {
                    var accountId = usersById.TryGetValue(s.UserId, out var user) ? user.Username : (s.StudentNumber ?? string.Empty);
                    var accountStatus = usersById.TryGetValue(s.UserId, out var userForStatus)
                        ? userForStatus.Status.ToString()
                        : "UNLINKED";
                    var gradeLabel = _gradeLevels.FirstOrDefault(g => g.Id == s.PreferredGradeLevelId)?.Code
                        ?? _gradeLevels.FirstOrDefault(g => g.Id == s.PreferredGradeLevelId)?.Name
                        ?? "(Not set)";
                    var curriculumLabel = (s.PreferredCurriculumId.HasValue && curriculumById.TryGetValue(s.PreferredCurriculumId.Value, out var cName))
                        ? cName
                        : "(Not set)";
                    var enrollmentStatus = enrollmentsByStudentId.TryGetValue(s.Id, out var enrollment)
                        ? enrollment.Status.ToString()
                        : "Not Enrolled";
                    var recordStatus = s.Status == UserStatus.INACTIVE ? "Archived" : "Active";

                    if (selectedGradeId > 0 && s.PreferredGradeLevelId != selectedGradeId)
                    {
                        continue;
                    }

                    if (!string.Equals(statusFilter, "All Statuses", StringComparison.OrdinalIgnoreCase))
                    {
                        var includeStatus = statusFilter switch
                        {
                            "Archived" => s.Status == UserStatus.INACTIVE,
                            "Active" => s.Status == UserStatus.ACTIVE,
                            "Inactive" => s.Status == UserStatus.INACTIVE,
                            "Locked" => usersById.TryGetValue(s.UserId, out var userStatus) && userStatus.Status == UserStatus.LOCKED,
                            _ => true
                        };

                        if (!includeStatus)
                        {
                            continue;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(term))
                    {
                        var matches =
                            (s.StudentNumber ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (s.Lrn ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            accountId.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (s.FirstName ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (s.LastName ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (s.GuardianContact ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            enrollmentStatus.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            accountStatus.Contains(term, StringComparison.OrdinalIgnoreCase);
                        if (!matches)
                        {
                            continue;
                        }
                    }

                    _studentsTable.Rows.Add(
                        s.Id,
                        s.StudentNumber,
                        s.Lrn,
                        $"{s.LastName}, {s.FirstName}{(string.IsNullOrWhiteSpace(s.MiddleName) ? string.Empty : $" {s.MiddleName}")}",
                        gradeLabel,
                        curriculumLabel,
                        s.Sex?.ToString() ?? string.Empty,
                        s.ContactNo ?? string.Empty,
                        s.GuardianName ?? string.Empty,
                        enrollmentStatus,
                        accountStatus,
                        recordStatus);
                }

                gridStudents.ItemsSource = _studentsTable.DefaultView;
                ApplyGridSort("students", _studentsTable.DefaultView);
                if (preferredId.HasValue)
                {
                    SelectStudentById(preferredId.Value);
                }

                UpdateStudentsWorkspaceInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Students load failed: {ex.Message}", "Students", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectStudentById(long studentId)
        {
            foreach (var item in gridStudents.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == studentId)
                {
                    gridStudents.SelectedItem = item;
                    gridStudents.ScrollIntoView(item);
                    return;
                }
            }
        }

        private void GridStudents_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Id")
            {
                e.Cancel = true;
                return;
            }

            if (e.PropertyName == "StudentNo")
            {
                e.Column.Header = "Student No";
            }

            if (e.PropertyName == "FullName")
            {
                e.Column.Header = "Full Name";
            }

            if (e.PropertyName == "GradeLevel")
            {
                e.Column.Header = "Grade Level";
            }

            if (e.PropertyName == "ContactNo")
            {
                e.Column.Header = "Contact No.";
            }

            if (e.PropertyName == "EnrollmentStatus")
            {
                e.Column.Header = "Enrollment Status";
            }

            if (e.PropertyName == "AccountStatus")
            {
                e.Column.Header = "Account Status";
            }

            if (e.PropertyName == "RecordStatus")
            {
                e.Column.Header = "Record Status";
            }
        }

        private void GridStudents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridStudents.SelectedItem is not DataRowView row)
            {
                _selectedStudentId = null;
                btnStudentArchiveRestore.Content = "Archive/Restore";
                UpdateStudentsWorkspaceInfo();
                return;
            }

            _selectedStudentId = row.Row.Field<long>("Id");
            var student = _students.FirstOrDefault(x => x.Id == _selectedStudentId.Value);
            if (student == null)
            {
                UpdateStudentsWorkspaceInfo();
                return;
            }

            var user = _userService.GetById(student.UserId);
            // Use the cached enrollment list populated during LoadStudents — no extra DB round-trip
            var latestEnrollment = _cachedEnrollments
                .Where(x => x.StudentId == student.Id)
                .OrderByDescending(x => x.UpdatedAt)
                .ThenByDescending(x => x.CreatedAt)
                .FirstOrDefault();

            _suppressStudentEvents = true;
            txtStudentNumber.Text = student.StudentNumber;
            txtStudentLrn.Text = student.Lrn;
            txtStudentProfileImage.Text = student.ProfileImageUrl ?? string.Empty;
            txtStudentAccountId.Text = user?.Username ?? student.StudentNumber;
            txtStudentAccountStatus.Text = user?.Status.ToString() ?? "UNLINKED";
            txtStudentEnrollmentStatus.Text = latestEnrollment?.Status.ToString() ?? "Not Enrolled";
            txtStudentRecordStatus.Text = student.Status == UserStatus.INACTIVE ? "Archived" : "Active";
            txtStudentFirst.Text = student.FirstName;
            txtStudentLast.Text = student.LastName;
            txtStudentMiddle.Text = student.MiddleName ?? string.Empty;
            dpStudentBirthdate.SelectedDate = student.Birthdate?.Date;
            cboStudentSex.SelectedItem = student.Sex;
            txtStudentAddress.Text = student.Address ?? string.Empty;
            txtStudentContactNo.Text = student.ContactNo ?? string.Empty;
            txtStudentGuardianName.Text = student.GuardianName ?? string.Empty;
            txtStudentGuardianContact.Text = student.GuardianContact ?? string.Empty;
            txtStudentPreviousSchool.Text = student.PreviousSchool ?? string.Empty;
            cboStudentPreferredGrade.SelectedValue = student.PreferredGradeLevelId ?? 0L;
            cboStudentPreferredCurriculum.SelectedValue = student.PreferredCurriculumId ?? 0L;
            cboStudentStatus.SelectedItem = student.Status;
            btnStudentArchiveRestore.Content = student.Status == UserStatus.INACTIVE ? "Restore" : "Archive";
            _suppressStudentEvents = false;
            UpdateStudentsWorkspaceInfo();
        }

        private void ArchiveOrRestoreStudent()
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student first.", "Archive / Restore", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var student = _studentService.GetById(_selectedStudentId.Value);
            if (student == null)
            {
                MessageBox.Show("Student record not found.", "Archive / Restore", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (student.Status == UserStatus.INACTIVE)
                {
                    if (!AppFeedbackService.Confirm("Restore selected student record?", "Confirm Restore", this))
                    {
                        return;
                    }

                    var oldData = new { student.Status };
                    student.Status = UserStatus.ACTIVE;
                    student.UpdatedAt = DateTime.UtcNow;
                    _studentService.Update(student);

                    var syncResult = _studentAccountService.SyncStudentAccount(student.Id);
                    if (!syncResult.Success)
                    {
                        AppFeedbackService.ShowWarning(syncResult.Message, "Restore Student", this);
                        return;
                    }

                    AuditTrailService.Log("RESTORE", "students", student.Id, oldData, new { student.Status });
                    AuditTrailService.Log("RESTORE", "student_accounts", student.Id, null, new
                    {
                        StudentId = student.Id,
                        UserId = student.UserId,
                        AccountId = syncResult.Data?.Username ?? student.StudentNumber,
                        Status = syncResult.Data?.Status ?? student.Status
                    });
                    AppFeedbackService.ShowSuccess(
                        $"Student restored successfully: {student.LastName}, {student.FirstName} ({student.StudentNumber}).",
                        "Restore Student",
                        this);
                    LoadStudents(student.Id);
                    return;
                }

                if (!AppFeedbackService.Confirm("Archive selected student record?", "Confirm Archive", this))
                {
                    return;
                }

                var user = _userService.GetById(student.UserId);
                AuditTrailService.Log("DELETE", "students", student.Id, student, null);
                AuditTrailService.Log("DELETE", "student_accounts", student.Id, new
                {
                    StudentId = student.Id,
                    UserId = student.UserId,
                    AccountId = user?.Username ?? student.StudentNumber
                }, null);
                _studentService.Delete(student.Id);

                if (user != null)
                {
                    AuditTrailService.Log("DELETE", "users", user.Id, user, null);
                    _userService.Delete(user.Id);
                }

                AppFeedbackService.ShowSuccess(
                    $"Student archived successfully: {student.LastName}, {student.FirstName} ({student.StudentNumber}).",
                    "Archive Student",
                    this);
                LoadStudents();
                ClearStudentEditor();
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError("Student operation failed.", ex, "Student", this);
            }
        }

        private void ResetStudentAccount()
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student first.", "Reset Account", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = _studentAccountService.ResetStudentAccount(_selectedStudentId.Value);
            if (!result.Success)
            {
                AppFeedbackService.ShowWarning(result.Message, "Reset Account", this);
                return;
            }

            AppFeedbackService.ShowSuccess(result.Message, "Reset Account", this);
            LoadStudents(_selectedStudentId);
        }

        private void OpenStudentHistory()
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student first.", "History", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var student = _studentService.GetById(_selectedStudentId.Value);
            if (student == null)
            {
                MessageBox.Show("Student record not found.", "History", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var label = $"Student {student.LastName}, {student.FirstName} ({student.Lrn})";
            new UserActivityHistoryWindow(_currentUser, "students", student.Id, label) { Owner = this }.ShowDialog();
        }

        private void OpenStudentRequirements()
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student first.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            new StudentRequirementsWindow(_selectedStudentId.Value) { Owner = this }.ShowDialog();
        }

        private void OpenStudentAccounts()
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student first.", "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            new StudentAccountsWindow(_currentUser, _selectedStudentId.Value) { Owner = this }.ShowDialog();
        }

        private void OpenStudentAccountHistory()
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student first.", "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var student = _studentService.GetById(_selectedStudentId.Value);
            if (student == null)
            {
                MessageBox.Show("Student record not found.", "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var label = $"Student Account {student.LastName}, {student.FirstName} ({student.StudentNumber})";
            new UserActivityHistoryWindow(_currentUser, "student_accounts", student.Id, label) { Owner = this }.ShowDialog();
        }

        private void ClearStudentEditor()
        {
            ResetStudentValidationState();
            _selectedStudentId = null;
            txtStudentNumber.Text = _schoolSettingService.PeekNextStudentNumber();
            txtStudentLrn.Clear();
            txtStudentProfileImage.Clear();
            txtStudentAccountId.Clear();
            txtStudentAccountStatus.Text = "Pending account";
            txtStudentEnrollmentStatus.Text = "Not Enrolled";
            txtStudentRecordStatus.Text = "New record";
            txtStudentFirst.Clear();
            txtStudentLast.Clear();
            txtStudentMiddle.Clear();
            dpStudentBirthdate.SelectedDate = null;
            cboStudentSex.SelectedIndex = -1;
            txtStudentAddress.Clear();
            txtStudentContactNo.Clear();
            txtStudentGuardianName.Clear();
            txtStudentGuardianContact.Clear();
            txtStudentPreviousSchool.Clear();
            cboStudentPreferredGrade.SelectedValue = 0L;
            cboStudentPreferredCurriculum.SelectedValue = 0L;
            cboStudentStatus.SelectedItem = UserStatus.ACTIVE;
            gridStudents.SelectedItem = null;
            btnStudentArchiveRestore.Content = "Archive/Restore";
            UpdateStudentsWorkspaceInfo();
        }

        private void ResetStudentValidationState()
        {
            HideValidationSummary(studentValidationSummaryHost, txtStudentValidationSummary);
            SetInputValidationState(txtStudentLrn, false);
            SetInputValidationState(txtStudentFirst, false);
            SetInputValidationState(txtStudentLast, false);
            SetInputValidationState(dpStudentBirthdate, false);
        }

        private void SetStudentViewerMode()
        {
            txtStudentLrn.IsReadOnly = true;
            txtStudentProfileImage.IsReadOnly = true;
            txtStudentFirst.IsReadOnly = true;
            txtStudentLast.IsReadOnly = true;
            txtStudentMiddle.IsReadOnly = true;
            dpStudentBirthdate.IsEnabled = false;
            cboStudentSex.IsEnabled = false;
            txtStudentAddress.IsReadOnly = true;
            txtStudentContactNo.IsReadOnly = true;
            txtStudentGuardianName.IsReadOnly = true;
            txtStudentGuardianContact.IsReadOnly = true;
            txtStudentPreviousSchool.IsReadOnly = true;
            cboStudentPreferredGrade.IsEnabled = false;
            cboStudentPreferredCurriculum.IsEnabled = false;
            cboStudentStatus.IsEnabled = false;
        }

        private static string? NullIfWhite(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private void UpdateStudentsWorkspaceInfo()
        {
            var total = _students.Count;
            var visible = _studentsTable?.Rows?.Count ?? 0;
            if (!_selectedStudentId.HasValue)
            {
                txtStudentsWorkspaceInfo.Text = $"Showing {visible} of {total} student records. Use grade and status filters to narrow the browse table, then select a row to review details or launch the edit modal.";
                return;
            }

            var selected = _students.FirstOrDefault(x => x.Id == _selectedStudentId.Value);
            if (selected == null)
            {
                txtStudentsWorkspaceInfo.Text = $"Showing {visible} of {total} student records.";
                return;
            }

            txtStudentsWorkspaceInfo.Text = $"Showing {visible} of {total}. Selected: {selected.LastName}, {selected.FirstName} ({selected.Lrn})";
        }

        private static long? GetSelectedLookupId(ComboBox comboBox)
        {
            return comboBox.SelectedValue is long id && id > 0 ? id : null;
        }

        private sealed class LookupChoice
        {
            public LookupChoice(long id, string label)
            {
                Id = id;
                Label = label;
            }

            public long Id { get; }
            public string Label { get; }
        }
    }
}

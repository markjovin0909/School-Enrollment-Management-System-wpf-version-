using System;
using System.Data;
using System.Collections.Generic;
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
        private string _lastAutoTeacherUsername = string.Empty;

        private static readonly string[] AdvisoryStatuses = { "UNASSIGNED", "ASSIGNED" };
        private static readonly string[] EmploymentStatuses = { "REGULAR", "PROBATIONARY", "PART_TIME", "CONTRACTUAL" };

        private void InitializeTeachersTab()
        {
            cboTeacherStatus.ItemsSource = Enum.GetValues(typeof(UserStatus));
            cboTeacherAdvisoryStatus.ItemsSource = AdvisoryStatuses;
            cboTeacherEmploymentStatus.ItemsSource = EmploymentStatuses;
            txtTeacherSearch.Text = GetSessionState("teachers.search");
            cboTeacherEmploymentFilter.ItemsSource = new[] { "All Employment", "REGULAR", "PROBATIONARY", "PART_TIME", "CONTRACTUAL" };
            cboTeacherEmploymentFilter.SelectedIndex = 0;

            btnTeachersNew.Click += (_, _) => OpenCreateTeacherDialog();
            btnTeachersRefresh.Click += (_, _) => LoadTeachers();
            btnTeacherAdd.Click += (_, _) => OpenCreateTeacherDialog();
            btnTeacherSave.Click += (_, _) => OpenEditTeacherDialog();
            btnTeacherArchiveRestore.Click += (_, _) => ArchiveOrRestoreTeacher();
            btnTeacherResetPassword.Click += (_, _) => ResetTeacherPassword();
            btnTeacherHistory.Click += (_, _) => OpenTeacherHistory();
            btnTeacherClear.Click += (_, _) => ClearTeacherEditor();
            btnTeacherSave.Content = "Edit Selected";

            txtTeacherSearch.TextChanged += (_, _) =>
            {
                if (_suppressTeacherEvents) return;
                SetSessionState("teachers.search", txtTeacherSearch.Text);
                LoadTeachers();
            };
            cboTeacherSpecializationFilter.SelectionChanged += (_, _) =>
            {
                if (_suppressTeacherEvents) return;
                LoadTeachers();
            };
            cboTeacherEmploymentFilter.SelectionChanged += (_, _) =>
            {
                if (_suppressTeacherEvents) return;
                LoadTeachers();
            };
            txtTeacherEmployeeNo.TextChanged += (_, _) => AutoFillTeacherUsernameFromEmployeeNo();
            gridTeachers.AutoGeneratingColumn += GridTeachers_AutoGeneratingColumn;
            gridTeachers.SelectionChanged += GridTeachers_SelectionChanged;
            WireGridSortPersistence(gridTeachers, "teachers");

            SetTeacherViewerMode();
            ClearTeacherEditor();
            LoadTeachers();
        }

        private void OpenCreateTeacherDialog()
        {
            var dialog = new TeacherCreateWindow { Owner = this };
            if (dialog.ShowDialog() == true && dialog.SavedTeacherId.HasValue)
            {
                LoadTeachers(dialog.SavedTeacherId.Value);
            }
        }

        private void OpenEditTeacherDialog()
        {
            if (!_selectedTeacherId.HasValue)
            {
                MessageBox.Show("Select a teacher first.", "Edit Teacher", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new TeacherCreateWindow(_selectedTeacherId.Value) { Owner = this };
                if (dialog.ShowDialog() == true && dialog.SavedTeacherId.HasValue)
                {
                    LoadTeachers(dialog.SavedTeacherId.Value);
                }
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError("Open edit teacher dialog failed.", ex, "Edit Teacher", this);
            }
        }

        private void AutoFillTeacherUsernameFromEmployeeNo()
        {
            if (_suppressTeacherEvents)
            {
                return;
            }

            var employeeNo = txtTeacherEmployeeNo.Text.Trim();
            if (string.IsNullOrWhiteSpace(employeeNo))
            {
                return;
            }

            var currentUsername = txtTeacherUsername.Text.Trim();
            if (string.IsNullOrWhiteSpace(currentUsername) || string.Equals(currentUsername, _lastAutoTeacherUsername, StringComparison.OrdinalIgnoreCase))
            {
                txtTeacherUsername.Text = employeeNo;
                _lastAutoTeacherUsername = employeeNo;
            }
        }

        private void LoadTeachers(long? preferredId = null)
        {
            try
            {
                _teachers = _teacherService.GetAll().ToList();
                var usersById = _userService.GetAll().ToDictionary(u => u.Id, u => u);
                var specializationOptions = new List<string> { "All Specializations" };
                specializationOptions.AddRange(_teachers
                    .Select(t => string.IsNullOrWhiteSpace(t.Specialization) ? "General" : t.Specialization.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x));
                var currentSpecialization = cboTeacherSpecializationFilter.SelectedItem as string;
                cboTeacherSpecializationFilter.ItemsSource = specializationOptions;
                cboTeacherSpecializationFilter.SelectedItem = specializationOptions.Contains(currentSpecialization ?? string.Empty)
                    ? currentSpecialization
                    : specializationOptions.FirstOrDefault();

                _teachersTable = new DataTable();
                _teachersTable.Columns.Add("Id", typeof(long));
                _teachersTable.Columns.Add("EmployeeNo");
                _teachersTable.Columns.Add("FullName");
                _teachersTable.Columns.Add("Email");
                _teachersTable.Columns.Add("Contact");
                _teachersTable.Columns.Add("Specialization");
                _teachersTable.Columns.Add("AdvisoryStatus");
                _teachersTable.Columns.Add("EmploymentStatus");
                _teachersTable.Columns.Add("AccountStatus");
                _teachersTable.Columns.Add("RecordStatus");

                var term = (txtTeacherSearch.Text ?? string.Empty).Trim();
                var specializationFilter = (cboTeacherSpecializationFilter.SelectedItem as string ?? "All Specializations").Trim();
                var employmentFilter = (cboTeacherEmploymentFilter.SelectedItem as string ?? "All Employment").Trim();
                foreach (var t in _teachers)
                {
                    var username = usersById.TryGetValue(t.UserId, out var user) ? user.Username : string.Empty;
                    var specialization = string.IsNullOrWhiteSpace(t.Specialization) ? "General" : t.Specialization.Trim();
                    var advisoryStatus = string.IsNullOrWhiteSpace(t.AdvisoryAssignmentStatus) ? AdvisoryStatuses[0] : t.AdvisoryAssignmentStatus.Trim();
                    var employmentStatus = string.IsNullOrWhiteSpace(t.EmploymentStatus) ? EmploymentStatuses[0] : t.EmploymentStatus.Trim();
                    var accountStatus = usersById.TryGetValue(t.UserId, out var accountUser) ? accountUser.Status.ToString() : "UNLINKED";
                    var recordStatus = t.Status == UserStatus.INACTIVE ? "Archived" : "Active";

                    if (!string.Equals(specializationFilter, "All Specializations", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(specialization, specializationFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.Equals(employmentFilter, "All Employment", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(employmentStatus, employmentFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(term))
                    {
                        var matches =
                            (t.EmployeeNo ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            username.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (t.FirstName ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (t.LastName ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (t.Email ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            specialization.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            employmentStatus.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            accountStatus.Contains(term, StringComparison.OrdinalIgnoreCase);
                        if (!matches)
                        {
                            continue;
                        }
                    }

                    _teachersTable.Rows.Add(
                        t.Id,
                        t.EmployeeNo ?? string.Empty,
                        $"{t.LastName}, {t.FirstName}{(string.IsNullOrWhiteSpace(t.MiddleName) ? string.Empty : $" {t.MiddleName}")}",
                        t.Email ?? string.Empty,
                        t.ContactNo ?? string.Empty,
                        specialization,
                        advisoryStatus,
                        employmentStatus,
                        accountStatus,
                        recordStatus);
                }

                gridTeachers.ItemsSource = _teachersTable.DefaultView;
                ApplyGridSort("teachers", _teachersTable.DefaultView);
                if (preferredId.HasValue)
                {
                    SelectTeacherById(preferredId.Value);
                }

                UpdateTeachersWorkspaceInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Teachers load failed: {ex.Message}", "Teachers", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectTeacherById(long teacherId)
        {
            foreach (var item in gridTeachers.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == teacherId)
                {
                    gridTeachers.SelectedItem = item;
                    gridTeachers.ScrollIntoView(item);
                    return;
                }
            }
        }

        private void GridTeachers_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Id")
            {
                e.Cancel = true;
                return;
            }

            if (e.PropertyName == "EmployeeNo")
            {
                e.Column.Header = "Employee No";
            }

            if (e.PropertyName == "FullName")
            {
                e.Column.Header = "Full Name";
            }

            if (e.PropertyName == "EmploymentStatus")
            {
                e.Column.Header = "Employment Status";
            }

            if (e.PropertyName == "AdvisoryStatus")
            {
                e.Column.Header = "Advisory Status";
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

        private void GridTeachers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridTeachers.SelectedItem is not DataRowView row)
            {
                _selectedTeacherId = null;
                btnTeacherArchiveRestore.Content = "Archive/Restore";
                UpdateTeacherProfileHeader(null);
                UpdateTeachersWorkspaceInfo();
                return;
            }

            ResetTeacherValidationState();
            _selectedTeacherId = row.Row.Field<long>("Id");
            var teacher = _teachers.FirstOrDefault(x => x.Id == _selectedTeacherId.Value);
            if (teacher == null)
            {
                UpdateTeachersWorkspaceInfo();
                return;
            }

            var user = _userService.GetById(teacher.UserId);
            _suppressTeacherEvents = true;
            txtTeacherEmployeeNo.Text = teacher.EmployeeNo ?? string.Empty;
            txtTeacherUsername.Text = user?.Username ?? string.Empty;
            txtTeacherProfileImage.Text = teacher.ProfileImageUrl ?? string.Empty;
            txtTeacherAccountStatus.Text = user?.Status.ToString() ?? "UNLINKED";
            txtTeacherRecordStatus.Text = teacher.Status == UserStatus.INACTIVE ? "Archived" : "Active";
            txtTeacherAdvisorySummary.Text = string.IsNullOrWhiteSpace(teacher.AdvisoryAssignmentStatus) ? AdvisoryStatuses[0] : teacher.AdvisoryAssignmentStatus;
            txtTeacherInitialPassword.Password = "Managed separately";
            txtTeacherFirst.Text = teacher.FirstName;
            txtTeacherLast.Text = teacher.LastName;
            txtTeacherMiddle.Text = teacher.MiddleName ?? string.Empty;
            txtTeacherEmail.Text = teacher.Email ?? string.Empty;
            txtTeacherContact.Text = teacher.ContactNo ?? string.Empty;
            txtTeacherSpecialization.Text = teacher.Specialization ?? string.Empty;
            cboTeacherAdvisoryStatus.SelectedItem = string.IsNullOrWhiteSpace(teacher.AdvisoryAssignmentStatus) ? AdvisoryStatuses[0] : teacher.AdvisoryAssignmentStatus;
            cboTeacherEmploymentStatus.SelectedItem = string.IsNullOrWhiteSpace(teacher.EmploymentStatus) ? EmploymentStatuses[0] : teacher.EmploymentStatus;
            dpTeacherHireDate.SelectedDate = teacher.HireDate?.Date;
            cboTeacherStatus.SelectedItem = teacher.Status;
            btnTeacherArchiveRestore.Content = teacher.Status == UserStatus.INACTIVE ? "Restore" : "Archive";
            UpdateTeacherProfileHeader(teacher);
            _suppressTeacherEvents = false;
            UpdateTeachersWorkspaceInfo();
        }

        private void UpdateTeacherProfileHeader(Teacher? teacher)
        {
            if (teacher == null)
            {
                txtTeacherProfileInitials.Text = "TC";
                txtTeacherProfileName.Text = "Select a teacher";
                txtTeacherProfileMeta.Text = "Choose a row to view personal information";
                return;
            }

            var fullName = $"{teacher.LastName}, {teacher.FirstName}{(string.IsNullOrWhiteSpace(teacher.MiddleName) ? "" : $" {teacher.MiddleName}")}";
            txtTeacherProfileInitials.Text = BuildPersonInitials(teacher.FirstName, teacher.LastName);
            txtTeacherProfileName.Text = fullName;
            var employee = string.IsNullOrWhiteSpace(teacher.EmployeeNo) ? "N/A" : teacher.EmployeeNo;
            var specialization = string.IsNullOrWhiteSpace(teacher.Specialization) ? "No specialization" : teacher.Specialization;
            txtTeacherProfileMeta.Text = $"Employee No: {employee}  ·  {specialization}";
        }

        private void ArchiveOrRestoreTeacher()
        {
            if (!_selectedTeacherId.HasValue)
            {
                MessageBox.Show("Select a teacher first.", "Archive / Restore", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var teacher = _teacherService.GetById(_selectedTeacherId.Value);
            if (teacher == null)
            {
                MessageBox.Show("Teacher record not found.", "Archive / Restore", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (teacher.Status == UserStatus.INACTIVE)
                {
                    if (!AppFeedbackService.Confirm("Restore selected teacher record?", "Confirm Restore", this))
                    {
                        return;
                    }

                    var oldData = new { teacher.Status };
                    teacher.Status = UserStatus.ACTIVE;
                    teacher.UpdatedAt = DateTime.UtcNow;
                    _teacherService.Update(teacher);

                    var restoreUser = _userService.GetById(teacher.UserId);
                    if (restoreUser != null)
                    {
                        var oldUser = new { restoreUser.Status };
                        restoreUser.Status = UserStatus.ACTIVE;
                        restoreUser.Role = UserRole.TEACHER;
                        restoreUser.CanLogin = false;
                        _userService.Update(restoreUser);
                        AuditTrailService.Log("RESTORE", "users", restoreUser.Id, oldUser, new { restoreUser.Status, restoreUser.Role, restoreUser.CanLogin });
                    }

                    AuditTrailService.Log("RESTORE", "teachers", teacher.Id, oldData, new { teacher.Status });
                    AppFeedbackService.ShowSuccess(
                        $"Teacher restored successfully: {teacher.LastName}, {teacher.FirstName} ({teacher.EmployeeNo}).",
                        "Restore Teacher",
                        this);
                    LoadTeachers(teacher.Id);
                    return;
                }

                if (!AppFeedbackService.Confirm("Archive selected teacher record?", "Confirm Archive", this))
                {
                    return;
                }

                var user = _userService.GetById(teacher.UserId);
                AuditTrailService.Log("DELETE", "teachers", teacher.Id, teacher, null);
                _teacherService.Delete(teacher.Id);

                if (user != null)
                {
                    AuditTrailService.Log("DELETE", "users", user.Id, user, null);
                    _userService.Delete(user.Id);
                }

                AppFeedbackService.ShowSuccess(
                    $"Teacher archived successfully: {teacher.LastName}, {teacher.FirstName} ({teacher.EmployeeNo}).",
                    "Archive Teacher",
                    this);
                LoadTeachers();
                ClearTeacherEditor();
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError("Teacher operation failed.", ex, "Teacher", this);
            }
        }

        private void ResetTeacherPassword()
        {
            if (!_selectedTeacherId.HasValue)
            {
                MessageBox.Show("Select a teacher first.", "Reset Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var teacher = _teacherService.GetById(_selectedTeacherId.Value);
            if (teacher == null)
            {
                return;
            }

            var dialog = new ChangePasswordWindow(teacher.UserId, requireCurrent: false) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                var user = _userService.GetById(teacher.UserId);
                if (user != null)
                {
                    AuditTrailService.Log("RESET_PASSWORD", "users", user.Id, null, new { user.Username, user.Role });
                }
            }
        }

        private void OpenTeacherHistory()
        {
            if (!_selectedTeacherId.HasValue)
            {
                MessageBox.Show("Select a teacher first.", "History", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var teacher = _teacherService.GetById(_selectedTeacherId.Value);
            if (teacher == null)
            {
                MessageBox.Show("Teacher record not found.", "History", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var label = $"Teacher {teacher.LastName}, {teacher.FirstName}";
            new UserActivityHistoryWindow(_currentUser, "teachers", teacher.Id, label) { Owner = this }.ShowDialog();
        }

        private void ClearTeacherEditor()
        {
            ResetTeacherValidationState();
            _suppressTeacherEvents = true;
            _selectedTeacherId = null;
            txtTeacherEmployeeNo.Clear();
            txtTeacherUsername.Clear();
            txtTeacherProfileImage.Clear();
            txtTeacherAccountStatus.Text = "Pending account";
            txtTeacherRecordStatus.Text = "New record";
            txtTeacherAdvisorySummary.Text = AdvisoryStatuses[0];
            txtTeacherFirst.Clear();
            txtTeacherLast.Clear();
            txtTeacherMiddle.Clear();
            txtTeacherEmail.Clear();
            txtTeacherContact.Clear();
            txtTeacherSpecialization.Clear();
            dpTeacherHireDate.SelectedDate = null;
            cboTeacherAdvisoryStatus.SelectedItem = AdvisoryStatuses[0];
            cboTeacherEmploymentStatus.SelectedItem = EmploymentStatuses[0];
            cboTeacherStatus.SelectedItem = UserStatus.ACTIVE;
            txtTeacherInitialPassword.Password = "ChangeMe123!";
            gridTeachers.SelectedItem = null;
            btnTeacherArchiveRestore.Content = "Archive/Restore";
            UpdateTeacherProfileHeader(null);
            _suppressTeacherEvents = false;
            _lastAutoTeacherUsername = string.Empty;
            UpdateTeachersWorkspaceInfo();
        }

        private void ResetTeacherValidationState()
        {
            HideValidationSummary(teacherValidationSummaryHost, txtTeacherValidationSummary);
            SetInputValidationState(txtTeacherEmployeeNo, false);
            SetInputValidationState(txtTeacherUsername, false);
            SetInputValidationState(txtTeacherFirst, false);
            SetInputValidationState(txtTeacherLast, false);
            SetInputValidationState(txtTeacherInitialPassword, false);
        }

        private void SetTeacherViewerMode()
        {
            txtTeacherEmployeeNo.IsReadOnly = true;
            txtTeacherUsername.IsReadOnly = true;
            txtTeacherProfileImage.IsReadOnly = true;
            txtTeacherFirst.IsReadOnly = true;
            txtTeacherLast.IsReadOnly = true;
            txtTeacherMiddle.IsReadOnly = true;
            txtTeacherEmail.IsReadOnly = true;
            txtTeacherContact.IsReadOnly = true;
            txtTeacherSpecialization.IsReadOnly = true;
            cboTeacherAdvisoryStatus.IsEnabled = false;
            cboTeacherEmploymentStatus.IsEnabled = false;
            dpTeacherHireDate.IsEnabled = false;
            cboTeacherStatus.IsEnabled = false;
            txtTeacherInitialPassword.IsEnabled = false;
        }

        private void UpdateTeachersWorkspaceInfo()
        {
            var total = _teachers.Count;
            var visible = _teachersTable?.Rows?.Count ?? 0;
            if (!_selectedTeacherId.HasValue)
            {
                txtTeachersWorkspaceInfo.Text = $"{visible} of {total}";
                return;
            }

            var selected = _teachers.FirstOrDefault(x => x.Id == _selectedTeacherId.Value);
            if (selected == null)
            {
                txtTeachersWorkspaceInfo.Text = $"{visible} of {total}";
                return;
            }

            txtTeachersWorkspaceInfo.Text = $"{visible} of {total} · {selected.LastName}, {selected.FirstName}";
        }
    }
}

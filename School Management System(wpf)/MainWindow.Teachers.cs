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

            btnTeachersRefresh.Click += (_, _) => LoadTeachers();
            btnTeacherAdd.Click += (_, _) => AddTeacher();
            btnTeacherSave.Click += (_, _) => SaveTeacher();
            btnTeacherArchiveRestore.Click += (_, _) => ArchiveOrRestoreTeacher();
            btnTeacherResetPassword.Click += (_, _) => ResetTeacherPassword();
            btnTeacherHistory.Click += (_, _) => OpenTeacherHistory();
            btnTeacherClear.Click += (_, _) => ClearTeacherEditor();

            txtTeacherSearch.TextChanged += (_, _) =>
            {
                if (_suppressTeacherEvents) return;
                SetSessionState("teachers.search", txtTeacherSearch.Text);
                LoadTeachers();
            };
            txtTeacherEmployeeNo.TextChanged += (_, _) => AutoFillTeacherUsernameFromEmployeeNo();
            gridTeachers.SelectionChanged += GridTeachers_SelectionChanged;
            WireGridSortPersistence(gridTeachers, "teachers");

            ClearTeacherEditor();
            LoadTeachers();
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

                _teachersTable = new DataTable();
                _teachersTable.Columns.Add("Id", typeof(long));
                _teachersTable.Columns.Add("EmployeeNo");
                _teachersTable.Columns.Add("Username");
                _teachersTable.Columns.Add("FirstName");
                _teachersTable.Columns.Add("LastName");
                _teachersTable.Columns.Add("Email");
                _teachersTable.Columns.Add("Specialization");
                _teachersTable.Columns.Add("EmploymentStatus");
                _teachersTable.Columns.Add("Status");

                var term = (txtTeacherSearch.Text ?? string.Empty).Trim();
                foreach (var t in _teachers)
                {
                    var username = usersById.TryGetValue(t.UserId, out var user) ? user.Username : string.Empty;
                    if (!string.IsNullOrWhiteSpace(term))
                    {
                        var matches =
                            (t.EmployeeNo ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            username.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (t.FirstName ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (t.LastName ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (t.Email ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (t.Specialization ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (t.EmploymentStatus ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            t.Status.ToString().Contains(term, StringComparison.OrdinalIgnoreCase);
                        if (!matches)
                        {
                            continue;
                        }
                    }

                    _teachersTable.Rows.Add(
                        t.Id,
                        t.EmployeeNo ?? string.Empty,
                        username,
                        t.FirstName,
                        t.LastName,
                        t.Email ?? string.Empty,
                        t.Specialization ?? string.Empty,
                        t.EmploymentStatus ?? string.Empty,
                        t.Status.ToString());
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

        private void GridTeachers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridTeachers.SelectedItem is not DataRowView row)
            {
                _selectedTeacherId = null;
                btnTeacherArchiveRestore.Content = "Archive/Restore";
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
            _suppressTeacherEvents = false;
            UpdateTeachersWorkspaceInfo();
        }

        private void AddTeacher()
        {
            long? createdUserId = null;
            ResetTeacherValidationState();
            try
            {
                var validationErrors = new List<string>();
                var employeeNo = txtTeacherEmployeeNo.Text.Trim();
                var username = txtTeacherUsername.Text.Trim();
                var firstName = txtTeacherFirst.Text.Trim();
                var lastName = txtTeacherLast.Text.Trim();
                var initialPassword = txtTeacherInitialPassword.Password;
                var status = cboTeacherStatus.SelectedItem is UserStatus selectedStatus ? selectedStatus : UserStatus.ACTIVE;

                SetInputValidationState(txtTeacherEmployeeNo, string.IsNullOrWhiteSpace(employeeNo));
                SetInputValidationState(txtTeacherUsername, string.IsNullOrWhiteSpace(username));
                SetInputValidationState(txtTeacherFirst, string.IsNullOrWhiteSpace(firstName));
                SetInputValidationState(txtTeacherLast, string.IsNullOrWhiteSpace(lastName));
                SetInputValidationState(txtTeacherInitialPassword, string.IsNullOrWhiteSpace(initialPassword));

                if (string.IsNullOrWhiteSpace(employeeNo) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    validationErrors.Add("Employee no, first name, and last name are required.");
                }

                if (string.IsNullOrWhiteSpace(username))
                {
                    validationErrors.Add("Account ID is required.");
                }

                if (string.IsNullOrWhiteSpace(initialPassword))
                {
                    validationErrors.Add("Initial password is required.");
                }

                var duplicateEmployee = _teacherService.GetAll().Any(t => string.Equals(t.EmployeeNo ?? string.Empty, employeeNo, StringComparison.OrdinalIgnoreCase));
                if (duplicateEmployee)
                {
                    validationErrors.Add("Employee number already exists.");
                    SetInputValidationState(txtTeacherEmployeeNo, true);
                }

                var duplicateUsername = _userService.GetAll().Any(u => string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
                if (duplicateUsername)
                {
                    validationErrors.Add("Account ID already exists.");
                    SetInputValidationState(txtTeacherUsername, true);
                }

                if (validationErrors.Count > 0)
                {
                    ShowValidationSummary(teacherValidationSummaryHost, txtTeacherValidationSummary, validationErrors);
                    return;
                }

                var user = new User
                {
                    Username = username,
                    Role = UserRole.TEACHER,
                    CanLogin = false,
                    Status = status
                };

                var userResult = _authService.Register(user, initialPassword);
                if (!userResult.Success || userResult.Data == null)
                {
                    MessageBox.Show(userResult.Message, "Add Teacher", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                createdUserId = userResult.Data.Id;
                var teacher = new Teacher
                {
                    UserId = userResult.Data.Id,
                    ProfileImageUrl = NullIfWhite(txtTeacherProfileImage.Text),
                    EmployeeNo = employeeNo,
                    FirstName = firstName,
                    LastName = lastName,
                    MiddleName = NullIfWhite(txtTeacherMiddle.Text),
                    Email = NullIfWhite(txtTeacherEmail.Text),
                    ContactNo = NullIfWhite(txtTeacherContact.Text),
                    Specialization = string.IsNullOrWhiteSpace(txtTeacherSpecialization.Text) ? "General" : txtTeacherSpecialization.Text.Trim(),
                    AdvisoryAssignmentStatus = string.IsNullOrWhiteSpace(cboTeacherAdvisoryStatus.Text) ? AdvisoryStatuses[0] : cboTeacherAdvisoryStatus.Text.Trim(),
                    EmploymentStatus = string.IsNullOrWhiteSpace(cboTeacherEmploymentStatus.Text) ? EmploymentStatuses[0] : cboTeacherEmploymentStatus.Text.Trim(),
                    HireDate = dpTeacherHireDate.SelectedDate?.Date,
                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _teacherService.Create(teacher);
                AuditTrailService.Log("CREATE", "users", userResult.Data.Id, null, new
                {
                    userResult.Data.Username,
                    userResult.Data.Role,
                    userResult.Data.Status
                });
                AuditTrailService.Log("CREATE", "teachers", teacher.Id, null, teacher);
                HideValidationSummary(teacherValidationSummaryHost, txtTeacherValidationSummary);
                LoadTeachers(teacher.Id);
            }
            catch (DomainValidationException ex)
            {
                ShowValidationSummary(teacherValidationSummaryHost, txtTeacherValidationSummary, new[] { ex.Message });
            }
            catch (Exception ex)
            {
                if (createdUserId.HasValue)
                {
                    try
                    {
                        _userService.Delete(createdUserId.Value);
                    }
                    catch
                    {
                        // Best effort to avoid orphaned accounts.
                    }
                }

                MessageBox.Show($"Add teacher failed: {ex.Message}", "Add Teacher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveTeacher()
        {
            ResetTeacherValidationState();
            if (!_selectedTeacherId.HasValue)
            {
                MessageBox.Show("Select a teacher first.", "Update Teacher", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var teacher = _teacherService.GetById(_selectedTeacherId.Value);
                if (teacher == null)
                {
                    return;
                }

                var employeeNo = txtTeacherEmployeeNo.Text.Trim();
                var username = txtTeacherUsername.Text.Trim();
                var firstName = txtTeacherFirst.Text.Trim();
                var lastName = txtTeacherLast.Text.Trim();
                var validationErrors = new List<string>();

                SetInputValidationState(txtTeacherEmployeeNo, string.IsNullOrWhiteSpace(employeeNo));
                SetInputValidationState(txtTeacherUsername, string.IsNullOrWhiteSpace(username));
                SetInputValidationState(txtTeacherFirst, string.IsNullOrWhiteSpace(firstName));
                SetInputValidationState(txtTeacherLast, string.IsNullOrWhiteSpace(lastName));

                if (string.IsNullOrWhiteSpace(employeeNo) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    validationErrors.Add("Employee no, first name, and last name are required.");
                }

                if (string.IsNullOrWhiteSpace(username))
                {
                    validationErrors.Add("Account ID is required.");
                }

                var duplicateEmployee = _teacherService.GetAll().Any(t => t.Id != teacher.Id && string.Equals(t.EmployeeNo ?? string.Empty, employeeNo, StringComparison.OrdinalIgnoreCase));
                if (duplicateEmployee)
                {
                    validationErrors.Add("Employee number already exists.");
                    SetInputValidationState(txtTeacherEmployeeNo, true);
                }

                var user = _userService.GetById(teacher.UserId);
                if (user != null)
                {
                    var duplicateUsername = _userService.GetAll().Any(u => u.Id != user.Id && string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
                    if (duplicateUsername)
                    {
                        validationErrors.Add("Account ID already exists.");
                        SetInputValidationState(txtTeacherUsername, true);
                    }
                }

                if (validationErrors.Count > 0)
                {
                    ShowValidationSummary(teacherValidationSummaryHost, txtTeacherValidationSummary, validationErrors);
                    return;
                }

                var oldData = new
                {
                    teacher.EmployeeNo,
                    teacher.FirstName,
                    teacher.LastName,
                    teacher.MiddleName,
                    teacher.Email,
                    teacher.ContactNo,
                    teacher.Specialization,
                    teacher.AdvisoryAssignmentStatus,
                    teacher.EmploymentStatus,
                    teacher.HireDate,
                    teacher.Status
                };

                teacher.EmployeeNo = employeeNo;
                teacher.ProfileImageUrl = NullIfWhite(txtTeacherProfileImage.Text);
                teacher.FirstName = txtTeacherFirst.Text.Trim();
                teacher.LastName = txtTeacherLast.Text.Trim();
                teacher.MiddleName = NullIfWhite(txtTeacherMiddle.Text);
                teacher.Email = NullIfWhite(txtTeacherEmail.Text);
                teacher.ContactNo = NullIfWhite(txtTeacherContact.Text);
                teacher.Specialization = string.IsNullOrWhiteSpace(txtTeacherSpecialization.Text) ? "General" : txtTeacherSpecialization.Text.Trim();
                teacher.AdvisoryAssignmentStatus = string.IsNullOrWhiteSpace(cboTeacherAdvisoryStatus.Text) ? AdvisoryStatuses[0] : cboTeacherAdvisoryStatus.Text.Trim();
                teacher.EmploymentStatus = string.IsNullOrWhiteSpace(cboTeacherEmploymentStatus.Text) ? EmploymentStatuses[0] : cboTeacherEmploymentStatus.Text.Trim();
                teacher.HireDate = dpTeacherHireDate.SelectedDate?.Date;
                teacher.Status = cboTeacherStatus.SelectedItem is UserStatus selectedStatus ? selectedStatus : teacher.Status;
                teacher.UpdatedAt = DateTime.UtcNow;

                _teacherService.Update(teacher);
                AuditTrailService.Log("UPDATE", "teachers", teacher.Id, oldData, teacher);

                if (user != null)
                {
                    var oldUser = new { user.Username, user.Status };
                    user.Username = username;
                    user.Role = UserRole.TEACHER;
                    user.CanLogin = false;
                    user.Status = teacher.Status;
                    _userService.Update(user);
                    AuditTrailService.Log("UPDATE", "users", user.Id, oldUser, new { user.Username, user.Status, user.Role, user.CanLogin });
                }

                HideValidationSummary(teacherValidationSummaryHost, txtTeacherValidationSummary);
                LoadTeachers(teacher.Id);
            }
            catch (DomainValidationException ex)
            {
                ShowValidationSummary(teacherValidationSummaryHost, txtTeacherValidationSummary, new[] { ex.Message });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update teacher failed: {ex.Message}", "Update Teacher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                    var confirmRestore = MessageBox.Show("Restore selected teacher record?", "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirmRestore != MessageBoxResult.Yes)
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
                    LoadTeachers(teacher.Id);
                    return;
                }

                var confirm = MessageBox.Show("Archive selected teacher record?", "Confirm Archive", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes)
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

                LoadTeachers();
                ClearTeacherEditor();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Teacher operation failed: {ex.Message}", "Teacher", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void UpdateTeachersWorkspaceInfo()
        {
            var total = _teachers.Count;
            var visible = _teachersTable?.Rows?.Count ?? 0;
            if (!_selectedTeacherId.HasValue)
            {
                txtTeachersWorkspaceInfo.Text = $"Showing {visible} of {total} teacher records. Select a row to edit details.";
                return;
            }

            var selected = _teachers.FirstOrDefault(x => x.Id == _selectedTeacherId.Value);
            if (selected == null)
            {
                txtTeachersWorkspaceInfo.Text = $"Showing {visible} of {total} teacher records.";
                return;
            }

            txtTeachersWorkspaceInfo.Text = $"Showing {visible} of {total}. Selected: {selected.LastName}, {selected.FirstName} ({selected.EmployeeNo})";
        }
    }
}

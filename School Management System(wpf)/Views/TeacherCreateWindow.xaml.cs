using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class TeacherCreateWindow : Window
    {
        private enum EditorMode
        {
            Create,
            Edit
        }

        private readonly TeacherService _teacherService = new();
        private readonly UserService _userService = new();
        private readonly AuthService _authService = new();
        private readonly EditorMode _mode;
        private readonly long? _editTeacherId;

        private static readonly string[] AdvisoryStatuses = { "UNASSIGNED", "ASSIGNED" };
        private static readonly string[] EmploymentStatuses = { "REGULAR", "PROBATIONARY", "PART_TIME", "CONTRACTUAL" };

        private string _lastAutoTeacherUsername = string.Empty;

        public long? CreatedTeacherId { get; private set; }
        public long? SavedTeacherId { get; private set; }

        public TeacherCreateWindow()
            : this(EditorMode.Create, null)
        {
        }

        public TeacherCreateWindow(long teacherId)
            : this(EditorMode.Edit, teacherId)
        {
        }

        private TeacherCreateWindow(EditorMode mode, long? editTeacherId)
        {
            _mode = mode;
            _editTeacherId = editTeacherId;
            InitializeComponent();

            cboTeacherStatus.ItemsSource = Enum.GetValues(typeof(UserStatus));
            cboTeacherAdvisoryStatus.ItemsSource = AdvisoryStatuses;
            cboTeacherEmploymentStatus.ItemsSource = EmploymentStatuses;

            txtTeacherEmployeeNo.TextChanged += (_, _) => AutoFillTeacherUsernameFromEmployeeNo();
            btnCreate.Click += (_, _) => SaveTeacher();
            btnClear.Click += (_, _) => ResetEditor();
            btnCancel.Click += (_, _) => Close();

            if (_mode == EditorMode.Edit)
            {
                ConfigureEditMode();
            }
            else
            {
                ConfigureCreateMode();
            }
        }

        private void AutoFillTeacherUsernameFromEmployeeNo()
        {
            if (_mode == EditorMode.Edit)
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

        private void ConfigureCreateMode()
        {
            Title = "Create Teacher";
            txtDialogTitle.Text = "Create Teacher";
            txtDialogSubtitle.Text = "Enter personal and employment details. A managed account is linked automatically.";
            txtDialogInitials.Text = "TC";
            btnCreate.Content = "Create Teacher";
            btnClear.Visibility = Visibility.Visible;
            txtTeacherInitialPassword.Visibility = Visibility.Visible;
            ResetEditor();
        }

        private void ConfigureEditMode()
        {
            Title = "Edit Teacher";
            txtDialogTitle.Text = "Edit Teacher";
            txtDialogSubtitle.Text = "Update personal information and keep the linked account synchronized.";
            btnCreate.Content = "Save Changes";
            btnClear.Visibility = Visibility.Collapsed;
            txtTeacherInitialPassword.Visibility = Visibility.Collapsed;
            LoadTeacherForEdit();
        }

        private void SaveTeacher()
        {
            if (_mode == EditorMode.Edit)
            {
                UpdateTeacher();
                return;
            }

            CreateTeacher();
        }

        private void CreateTeacher()
        {
            long? createdUserId = null;
            ResetValidationState();

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
                    ShowValidationSummary(validationErrors);
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
                    AppFeedbackService.ShowError(userResult.Message, "Create Teacher", this);
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

                AppFeedbackService.ShowSuccess(
                    $"Teacher created successfully: {teacher.LastName}, {teacher.FirstName} ({teacher.EmployeeNo}).",
                    "Create Teacher",
                    this);
                CreatedTeacherId = teacher.Id;
                SavedTeacherId = teacher.Id;
                DialogResult = true;
                Close();
            }
            catch (DomainValidationException ex)
            {
                ShowValidationSummary(new[] { ex.Message });
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
                    }
                }

                AppFeedbackService.ShowError("Create teacher failed.", ex, "Create Teacher", this);
            }
        }

        private void UpdateTeacher()
        {
            ResetValidationState();

            if (!_editTeacherId.HasValue)
            {
                AppFeedbackService.ShowWarning("No teacher was supplied for edit mode.", "Edit Teacher", this);
                return;
            }

            try
            {
                var teacher = _teacherService.GetById(_editTeacherId.Value);
                if (teacher == null)
                {
                    AppFeedbackService.ShowWarning("Teacher record not found.", "Edit Teacher", this);
                    return;
                }

                var validationErrors = new List<string>();
                var employeeNo = txtTeacherEmployeeNo.Text.Trim();
                var username = txtTeacherUsername.Text.Trim();
                var firstName = txtTeacherFirst.Text.Trim();
                var lastName = txtTeacherLast.Text.Trim();

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

                var duplicateEmployee = _teacherService.GetAll().Any(t =>
                    t.Id != teacher.Id &&
                    string.Equals(t.EmployeeNo ?? string.Empty, employeeNo, StringComparison.OrdinalIgnoreCase));
                if (duplicateEmployee)
                {
                    validationErrors.Add("Employee number already exists.");
                    SetInputValidationState(txtTeacherEmployeeNo, true);
                }

                var user = _userService.GetById(teacher.UserId);
                if (user != null)
                {
                    var duplicateUsername = _userService.GetAll().Any(u =>
                        u.Id != user.Id &&
                        string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase));
                    if (duplicateUsername)
                    {
                        validationErrors.Add("Account ID already exists.");
                        SetInputValidationState(txtTeacherUsername, true);
                    }
                }

                if (validationErrors.Count > 0)
                {
                    ShowValidationSummary(validationErrors);
                    return;
                }

                var oldData = new
                {
                    teacher.EmployeeNo,
                    teacher.ProfileImageUrl,
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
                teacher.FirstName = firstName;
                teacher.LastName = lastName;
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

                AppFeedbackService.ShowSuccess(
                    $"Teacher updated successfully: {teacher.LastName}, {teacher.FirstName} ({teacher.EmployeeNo}).",
                    "Edit Teacher",
                    this);
                SavedTeacherId = teacher.Id;
                DialogResult = true;
                Close();
            }
            catch (DomainValidationException ex)
            {
                ShowValidationSummary(new[] { ex.Message });
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError("Update teacher failed.", ex, "Edit Teacher", this);
            }
        }

        private void LoadTeacherForEdit()
        {
            if (!_editTeacherId.HasValue)
            {
                AppFeedbackService.ShowWarning("No teacher was supplied for edit mode.", "Edit Teacher", this);
                Close();
                return;
            }

            var teacher = _teacherService.GetById(_editTeacherId.Value);
            if (teacher == null)
            {
                AppFeedbackService.ShowWarning("Teacher record not found.", "Edit Teacher", this);
                Close();
                return;
            }

            ResetValidationState();
            txtTeacherEmployeeNo.Text = teacher.EmployeeNo ?? string.Empty;
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

            var user = _userService.GetById(teacher.UserId);
            txtTeacherUsername.Text = user?.Username ?? string.Empty;
            txtTeacherInitialPassword.Password = string.Empty;
            txtDialogInitials.Text = BuildInitials(teacher.FirstName, teacher.LastName);
            txtDialogTitle.Text = $"{teacher.LastName}, {teacher.FirstName}";
            txtDialogSubtitle.Text = $"Employee No: {teacher.EmployeeNo ?? "N/A"}  ·  Edit personal information";
        }

        private static string BuildInitials(string? firstName, string? lastName)
        {
            var first = string.IsNullOrWhiteSpace(firstName) ? string.Empty : firstName.Trim()[0].ToString().ToUpperInvariant();
            var last = string.IsNullOrWhiteSpace(lastName) ? string.Empty : lastName.Trim()[0].ToString().ToUpperInvariant();
            var initials = $"{first}{last}";
            return string.IsNullOrWhiteSpace(initials) ? "??" : initials;
        }

        private void ResetEditor()
        {
            ResetValidationState();
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
            _lastAutoTeacherUsername = string.Empty;
        }

        private void ResetValidationState()
        {
            HideValidationSummary();
            SetInputValidationState(txtTeacherEmployeeNo, false);
            SetInputValidationState(txtTeacherUsername, false);
            SetInputValidationState(txtTeacherFirst, false);
            SetInputValidationState(txtTeacherLast, false);
            SetInputValidationState(txtTeacherInitialPassword, _mode == EditorMode.Create && string.IsNullOrWhiteSpace(txtTeacherInitialPassword.Password));
        }

        private void ShowValidationSummary(IEnumerable<string> errors)
        {
            var items = errors
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            txtValidationSummary.Text = string.Join(Environment.NewLine, items.Select(x => $"- {x}"));
            validationSummaryHost.Visibility = items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void HideValidationSummary()
        {
            validationSummaryHost.Visibility = Visibility.Collapsed;
            txtValidationSummary.Text = string.Empty;
        }

        private void SetInputValidationState(Control control, bool hasError)
        {
            var borderBrush = hasError
                ? (Brush)FindResource("Brush.Danger")
                : (Brush)FindResource("Brush.Border");

            control.BorderBrush = borderBrush;
            control.ToolTip = hasError ? "Please review this field." : null;
        }

        private static string? NullIfWhite(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}

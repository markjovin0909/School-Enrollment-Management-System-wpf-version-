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
        private readonly TeacherService _teacherService = new();
        private readonly UserService _userService = new();
        private readonly AuthService _authService = new();

        private static readonly string[] AdvisoryStatuses = { "UNASSIGNED", "ASSIGNED" };
        private static readonly string[] EmploymentStatuses = { "REGULAR", "PROBATIONARY", "PART_TIME", "CONTRACTUAL" };

        private string _lastAutoTeacherUsername = string.Empty;

        public long? CreatedTeacherId { get; private set; }

        public TeacherCreateWindow()
        {
            InitializeComponent();

            cboTeacherStatus.ItemsSource = Enum.GetValues(typeof(UserStatus));
            cboTeacherAdvisoryStatus.ItemsSource = AdvisoryStatuses;
            cboTeacherEmploymentStatus.ItemsSource = EmploymentStatuses;

            txtTeacherEmployeeNo.TextChanged += (_, _) => AutoFillTeacherUsernameFromEmployeeNo();
            btnCreate.Click += (_, _) => CreateTeacher();
            btnClear.Click += (_, _) => ResetEditor();
            btnCancel.Click += (_, _) => Close();

            ResetEditor();
        }

        private void AutoFillTeacherUsernameFromEmployeeNo()
        {
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
                    MessageBox.Show(userResult.Message, "Create Teacher", MessageBoxButton.OK, MessageBoxImage.Error);
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

                CreatedTeacherId = teacher.Id;
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

                MessageBox.Show($"Create teacher failed: {ex.Message}", "Create Teacher", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            SetInputValidationState(txtTeacherInitialPassword, false);
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

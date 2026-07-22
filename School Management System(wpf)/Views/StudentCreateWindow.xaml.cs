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
    public partial class StudentCreateWindow : Window
    {
        private enum EditorMode
        {
            Create,
            Edit
        }

        private readonly StudentService _studentService = new();
        private readonly UserService _userService = new();
        private readonly StudentAccountService _studentAccountService = new();
        private readonly SchoolSettingService _schoolSettingService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly CurriculumService _curriculumService = new();
        private readonly EditorMode _mode;
        private readonly long? _editStudentId;

        public long? CreatedStudentId { get; private set; }
        public long? SavedStudentId { get; private set; }

        public StudentCreateWindow()
            : this(EditorMode.Create, null)
        {
        }

        public StudentCreateWindow(long studentId)
            : this(EditorMode.Edit, studentId)
        {
        }

        private StudentCreateWindow(EditorMode mode, long? editStudentId)
        {
            _mode = mode;
            _editStudentId = editStudentId;
            InitializeComponent();

            try
            {
                cboStudentStatus.ItemsSource = Enum.GetValues(typeof(UserStatus));
                cboStudentSex.ItemsSource = Enum.GetValues(typeof(Sex));

                btnCreate.Click += (_, _) => SaveStudent();
                btnClear.Click += (_, _) => ResetEditor();
                btnCancel.Click += (_, _) => Close();

                LoadLookups();
                if (_mode == EditorMode.Edit)
                {
                    ConfigureEditMode();
                }
                else
                {
                    ConfigureCreateMode();
                }
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError("Student create window initialization failed.", ex, "Create Student", this);
                throw;
            }
        }

        private void LoadLookups()
        {
            var defaultGradeLevelIds = _schoolSettingService.GetDefaultGradeLevelIds().ToHashSet();
            var gradeItems = _schoolSettingService.OrderGradeLevelsByDefaultScope(_gradeLevelService.GetAll())
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
            var primaryDefault = _schoolSettingService.GetPrimaryDefaultGradeLevelId();
            cboStudentPreferredGrade.SelectedValue = primaryDefault.HasValue
                ? gradeItems.FirstOrDefault(x => x.Id == primaryDefault.Value)?.Id ?? 0L
                : defaultGradeLevelIds.Count > 0
                    ? gradeItems.FirstOrDefault(x => defaultGradeLevelIds.Contains(x.Id))?.Id ?? 0L
                    : 0L;

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

        private void ConfigureCreateMode()
        {
            Title = "Create Student";
            txtDialogTitle.Text = "Create Student";
            txtDialogSubtitle.Text = "Enter personal information. A managed account is created automatically.";
            txtDialogInitials.Text = "ST";
            btnCreate.Content = "Create Student";
            btnClear.Visibility = Visibility.Visible;
            ResetEditor();
        }

        private void ConfigureEditMode()
        {
            Title = "Edit Student";
            txtDialogTitle.Text = "Edit Student";
            txtDialogSubtitle.Text = "Update personal information and keep the linked account synchronized.";
            btnCreate.Content = "Save Changes";
            btnClear.Visibility = Visibility.Collapsed;
            LoadStudentForEdit();
        }

        private void SaveStudent()
        {
            if (_mode == EditorMode.Edit)
            {
                UpdateStudent();
                return;
            }

            CreateStudent();
        }

        private void CreateStudent()
        {
            long? createdUserId = null;
            ResetValidationState();

            try
            {
                var validationErrors = new List<string>();
                var lrn = txtStudentLrn.Text.Trim();
                var firstName = txtStudentFirst.Text.Trim();
                var lastName = txtStudentLast.Text.Trim();
                var birthdate = dpStudentBirthdate.SelectedDate?.Date;

                SetInputValidationState(txtStudentLrn, string.IsNullOrWhiteSpace(lrn));
                SetInputValidationState(txtStudentFirst, string.IsNullOrWhiteSpace(firstName));
                SetInputValidationState(txtStudentLast, string.IsNullOrWhiteSpace(lastName));
                if (string.IsNullOrWhiteSpace(lrn) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    validationErrors.Add("LRN, first name, and last name are required.");
                }

                var duplicateLrn = _studentService.GetAll().Any(s => string.Equals(s.Lrn, lrn, StringComparison.OrdinalIgnoreCase));
                if (duplicateLrn)
                {
                    validationErrors.Add("LRN already exists.");
                    SetInputValidationState(txtStudentLrn, true);
                }

                var duplicateNameBirthdate = _studentService.GetAll().Any(s =>
                    string.Equals(s.FirstName, firstName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.LastName, lastName, StringComparison.OrdinalIgnoreCase) &&
                    Nullable.Equals(s.Birthdate?.Date, birthdate));
                if (duplicateNameBirthdate)
                {
                    validationErrors.Add("A student with the same full name and birthdate already exists.");
                    SetInputValidationState(txtStudentFirst, true);
                    SetInputValidationState(txtStudentLast, true);
                    SetInputValidationState(dpStudentBirthdate, true);
                }

                if (validationErrors.Count > 0)
                {
                    ShowValidationSummary(validationErrors);
                    return;
                }

                var status = cboStudentStatus.SelectedItem is UserStatus selectedStatus ? selectedStatus : UserStatus.ACTIVE;
                var studentNumber = _schoolSettingService.ReserveNextStudentNumber();
                var accountResult = _studentAccountService.CreateManagedAccount(studentNumber, status);
                if (!accountResult.Success || accountResult.Data == null)
                {
                    AppFeedbackService.ShowError(accountResult.Message, "Create Student", this);
                    return;
                }

                createdUserId = accountResult.Data.Id;
                var student = new Student
                {
                    UserId = accountResult.Data.Id,
                    StudentNumber = studentNumber,
                    ProfileImageUrl = NullIfWhite(txtStudentProfileImage.Text),
                    Lrn = lrn,
                    FirstName = firstName,
                    LastName = lastName,
                    MiddleName = NullIfWhite(txtStudentMiddle.Text),
                    Sex = cboStudentSex.SelectedItem is Sex selectedSex ? selectedSex : null,
                    Birthdate = birthdate,
                    Age = ComputeAge(birthdate),
                    Address = NullIfWhite(txtStudentAddress.Text),
                    ContactNo = NullIfWhite(txtStudentContactNo.Text),
                    GuardianName = NullIfWhite(txtStudentGuardianName.Text),
                    GuardianContact = txtStudentGuardianContact.Text.Trim(),
                    PreviousSchool = NullIfWhite(txtStudentPreviousSchool.Text),
                    PreferredGradeLevelId = GetSelectedLookupId(cboStudentPreferredGrade),
                    PreferredCurriculumId = GetSelectedLookupId(cboStudentPreferredCurriculum),
                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _studentService.Create(student);
                AuditTrailService.Log("CREATE", "student_accounts", student.Id, null, new
                {
                    StudentId = student.Id,
                    UserId = accountResult.Data.Id,
                    AccountId = accountResult.Data.Username,
                    Status = accountResult.Data.Status,
                    accountResult.Data.CanLogin
                });
                AuditTrailService.Log("CREATE", "students", student.Id, null, student);

                AppFeedbackService.ShowSuccess(
                    $"Student created successfully: {student.LastName}, {student.FirstName} ({student.StudentNumber}).",
                    "Create Student",
                    this);
                CreatedStudentId = student.Id;
                SavedStudentId = student.Id;
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

                AppFeedbackService.ShowError("Create student failed.", ex, "Create Student", this);
            }
        }

        private void UpdateStudent()
        {
            ResetValidationState();

            if (!_editStudentId.HasValue)
            {
                AppFeedbackService.ShowWarning("No student was supplied for edit mode.", "Edit Student", this);
                return;
            }

            try
            {
                var student = _studentService.GetById(_editStudentId.Value);
                if (student == null)
                {
                    AppFeedbackService.ShowWarning("Student record not found.", "Edit Student", this);
                    return;
                }

                var validationErrors = new List<string>();
                var lrn = txtStudentLrn.Text.Trim();
                var firstName = txtStudentFirst.Text.Trim();
                var lastName = txtStudentLast.Text.Trim();
                var birthdate = dpStudentBirthdate.SelectedDate?.Date;

                SetInputValidationState(txtStudentLrn, string.IsNullOrWhiteSpace(lrn));
                SetInputValidationState(txtStudentFirst, string.IsNullOrWhiteSpace(firstName));
                SetInputValidationState(txtStudentLast, string.IsNullOrWhiteSpace(lastName));
                if (string.IsNullOrWhiteSpace(lrn) || string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    validationErrors.Add("LRN, first name, and last name are required.");
                }

                var students = _studentService.GetAll().ToList();
                var duplicateLrn = students.Any(s => s.Id != student.Id && string.Equals(s.Lrn, lrn, StringComparison.OrdinalIgnoreCase));
                if (duplicateLrn)
                {
                    validationErrors.Add("LRN already exists.");
                    SetInputValidationState(txtStudentLrn, true);
                }

                var duplicateNameBirthdate = students.Any(s =>
                    s.Id != student.Id &&
                    string.Equals(s.FirstName, firstName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.LastName, lastName, StringComparison.OrdinalIgnoreCase) &&
                    Nullable.Equals(s.Birthdate?.Date, birthdate));
                if (duplicateNameBirthdate)
                {
                    validationErrors.Add("A student with the same full name and birthdate already exists.");
                    SetInputValidationState(txtStudentFirst, true);
                    SetInputValidationState(txtStudentLast, true);
                    SetInputValidationState(dpStudentBirthdate, true);
                }

                if (validationErrors.Count > 0)
                {
                    ShowValidationSummary(validationErrors);
                    return;
                }

                var oldData = new
                {
                    student.Lrn,
                    student.ProfileImageUrl,
                    student.FirstName,
                    student.LastName,
                    student.MiddleName,
                    student.Sex,
                    student.Birthdate,
                    student.Address,
                    student.ContactNo,
                    student.GuardianName,
                    student.GuardianContact,
                    student.PreviousSchool,
                    student.PreferredGradeLevelId,
                    student.PreferredCurriculumId,
                    student.Status
                };

                student.Lrn = lrn;
                student.ProfileImageUrl = NullIfWhite(txtStudentProfileImage.Text);
                student.FirstName = firstName;
                student.LastName = lastName;
                student.MiddleName = NullIfWhite(txtStudentMiddle.Text);
                student.Sex = cboStudentSex.SelectedItem is Sex selectedSex ? selectedSex : null;
                student.Birthdate = birthdate;
                student.Age = ComputeAge(birthdate);
                student.Address = NullIfWhite(txtStudentAddress.Text);
                student.ContactNo = NullIfWhite(txtStudentContactNo.Text);
                student.GuardianName = NullIfWhite(txtStudentGuardianName.Text);
                student.GuardianContact = txtStudentGuardianContact.Text.Trim();
                student.PreviousSchool = NullIfWhite(txtStudentPreviousSchool.Text);
                student.PreferredGradeLevelId = GetSelectedLookupId(cboStudentPreferredGrade);
                student.PreferredCurriculumId = GetSelectedLookupId(cboStudentPreferredCurriculum);
                student.Status = cboStudentStatus.SelectedItem is UserStatus selectedStatus ? selectedStatus : student.Status;
                student.UpdatedAt = DateTime.UtcNow;

                _studentService.Update(student);
                AuditTrailService.Log("UPDATE", "students", student.Id, oldData, student);

                var syncResult = _studentAccountService.SyncStudentAccount(student.Id);
                if (!syncResult.Success)
                {
                    AppFeedbackService.ShowWarning(syncResult.Message, "Student Account", this);
                    return;
                }

                AppFeedbackService.ShowSuccess(
                    $"Student updated successfully: {student.LastName}, {student.FirstName} ({student.StudentNumber}).",
                    "Edit Student",
                    this);
                SavedStudentId = student.Id;
                DialogResult = true;
                Close();
            }
            catch (DomainValidationException ex)
            {
                ShowValidationSummary(new[] { ex.Message });
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError("Update student failed.", ex, "Edit Student", this);
            }
        }

        private void LoadStudentForEdit()
        {
            if (!_editStudentId.HasValue)
            {
                AppFeedbackService.ShowWarning("No student was supplied for edit mode.", "Edit Student", this);
                Close();
                return;
            }

            var student = _studentService.GetById(_editStudentId.Value);
            if (student == null)
            {
                AppFeedbackService.ShowWarning("Student record not found.", "Edit Student", this);
                Close();
                return;
            }

            ResetValidationState();
            txtStudentNumber.Text = student.StudentNumber;
            txtStudentLrn.Text = student.Lrn;
            txtStudentProfileImage.Text = student.ProfileImageUrl ?? string.Empty;
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
            txtDialogInitials.Text = BuildInitials(student.FirstName, student.LastName);
            txtDialogTitle.Text = $"{student.LastName}, {student.FirstName}";
            txtDialogSubtitle.Text = $"Student No: {student.StudentNumber}  ·  LRN: {student.Lrn}";
            cboStudentPreferredGrade.SelectedValue = student.PreferredGradeLevelId ?? 0L;
            cboStudentPreferredCurriculum.SelectedValue = student.PreferredCurriculumId ?? 0L;
            cboStudentStatus.SelectedItem = student.Status;
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
            txtStudentNumber.Text = _schoolSettingService.PeekNextStudentNumber();
            txtStudentLrn.Clear();
            txtStudentProfileImage.Clear();
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
            cboStudentPreferredCurriculum.SelectedValue = 0L;
            cboStudentStatus.SelectedItem = UserStatus.ACTIVE;
        }

        private void ResetValidationState()
        {
            HideValidationSummary();
            SetInputValidationState(txtStudentLrn, false);
            SetInputValidationState(txtStudentFirst, false);
            SetInputValidationState(txtStudentLast, false);
            SetInputValidationState(dpStudentBirthdate, false);
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

        private static int? ComputeAge(DateTime? birthdate)
        {
            if (!birthdate.HasValue)
            {
                return null;
            }

            var today = DateTime.Today;
            var age = today.Year - birthdate.Value.Year;
            if (birthdate.Value.Date > today.AddYears(-age))
            {
                age--;
            }

            return age < 0 ? null : age;
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

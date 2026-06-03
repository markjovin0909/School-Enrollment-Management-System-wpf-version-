using System;
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
        private const int StudentDetailsTabIndex = 9;

        private long? _studentDetailId;
        private Student? _studentDetailRecord;
        private bool _studentDetailEditMode;

        private void InitializeStudentDetailsTab()
        {
            cboSDStatus.ItemsSource = Enum.GetValues(typeof(UserStatus));
            cboSDSex.ItemsSource = Enum.GetValues(typeof(Sex));
            LoadStudentDetailLookups();

            btnStudentDetailEdit.Click += (_, _) => EnterStudentDetailEditMode();
            btnStudentDetailDelete.Click += (_, _) => DeleteStudentDetail();
            btnStudentDetailSave.Click += (_, _) => SaveStudentDetail();
            btnStudentDetailCancelEdit.Click += (_, _) => ExitStudentDetailEditMode();
            btnSDManageRequirements.Click += (_, _) => OpenStudentDetailRequirements();
        }

        private void LoadStudentDetailLookups()
        {
            var gradeItems = _gradeLevelService.GetAll()
                .OrderBy(g => g.Name)
                .Select(g => new LookupChoice(
                    g.Id,
                    string.IsNullOrWhiteSpace(g.Code) ? g.Name : $"{g.Code} - {g.Name}"))
                .ToList();
            gradeItems.Insert(0, new LookupChoice(0, "(Not set)"));

            cboSDPreferredGrade.DisplayMemberPath = nameof(LookupChoice.Label);
            cboSDPreferredGrade.SelectedValuePath = nameof(LookupChoice.Id);
            cboSDPreferredGrade.ItemsSource = gradeItems;

            var curriculumItems = _curriculumService.GetAll()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new LookupChoice(c.Id, c.Name))
                .ToList();
            curriculumItems.Insert(0, new LookupChoice(0, "(Not set)"));

            cboSDPreferredCurriculum.DisplayMemberPath = nameof(LookupChoice.Label);
            cboSDPreferredCurriculum.SelectedValuePath = nameof(LookupChoice.Id);
            cboSDPreferredCurriculum.ItemsSource = curriculumItems;
        }

        private void NavigateToStudentDetails(long studentId)
        {
            _studentDetailId = studentId;
            LoadStudentDetailData();
            NavigateMainTab(StudentDetailsTabIndex);
        }

        private void LoadStudentDetailData()
        {
            if (!_studentDetailId.HasValue) return;

            _studentDetailRecord = _studentService.GetById(_studentDetailId.Value);
            if (_studentDetailRecord == null)
            {
                MessageBox.Show("Student not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigateMainTab(0);
                return;
            }

            var fullName = $"{_studentDetailRecord.LastName}, {_studentDetailRecord.FirstName}{(string.IsNullOrWhiteSpace(_studentDetailRecord.MiddleName) ? "" : $" {_studentDetailRecord.MiddleName}")}";
            sectionStudentDetailHeader.Title = fullName;
            sectionStudentDetailHeader.Subtitle = $"LRN: {_studentDetailRecord.Lrn}  |  Student No: {_studentDetailRecord.StudentNumber}";

            PopulateStudentDetailFields();
            LoadStudentDetailRequirements();
            LoadStudentDetailGrades();
            LoadStudentDetailSubjects();
        }

        private void PopulateStudentDetailFields()
        {
            if (_studentDetailRecord == null) return;

            txtSDStudentNumber.Text = _studentDetailRecord.StudentNumber;
            txtSDLrn.Text = _studentDetailRecord.Lrn;
            txtSDFirstName.Text = _studentDetailRecord.FirstName;
            txtSDLastName.Text = _studentDetailRecord.LastName;
            txtSDMiddleName.Text = _studentDetailRecord.MiddleName ?? "";
            dpSDBirthdate.SelectedDate = _studentDetailRecord.Birthdate?.Date;
            txtSDAge.Text = _studentDetailRecord.Age?.ToString() ?? "";
            cboSDSex.SelectedItem = _studentDetailRecord.Sex;
            txtSDAddress.Text = _studentDetailRecord.Address ?? "";
            txtSDContactNo.Text = _studentDetailRecord.ContactNo ?? "";
            txtSDGuardianName.Text = _studentDetailRecord.GuardianName ?? "";
            txtSDGuardianContact.Text = _studentDetailRecord.GuardianContact ?? "";
            txtSDPreviousSchool.Text = _studentDetailRecord.PreviousSchool ?? "";
            cboSDPreferredGrade.SelectedValue = _studentDetailRecord.PreferredGradeLevelId ?? 0L;
            cboSDPreferredCurriculum.SelectedValue = _studentDetailRecord.PreferredCurriculumId ?? 0L;
            cboSDStatus.SelectedItem = _studentDetailRecord.Status;
        }

        private void LoadStudentDetailRequirements()
        {
            if (!_studentDetailId.HasValue) return;

            try
            {
                var requirementService = new StudentRequirementService();
                var requirements = requirementService.GetAll()
                    .Where(r => r.StudentId == _studentDetailId.Value)
                    .Select(r => new StudentDetailRequirementRow
                    {
                        Name = r.RequirementName,
                        Status = r.IsSubmitted ? "Submitted" : "Not Submitted",
                        SubmittedDate = r.SubmittedAt?.ToString("yyyy-MM-dd") ?? "",
                        Notes = r.Notes ?? ""
                    })
                    .ToList();

                gridSDRequirements.ItemsSource = requirements;
            }
            catch (Exception ex)
            {
                gridSDRequirements.ItemsSource = null;
                System.Diagnostics.Debug.WriteLine($"Load requirements failed: {ex.Message}");
            }
        }

        private void LoadStudentDetailGrades()
        {
            if (!_studentDetailId.HasValue) return;

            try
            {
                var gradeService = new StudentGradeService();
                var gradingPeriodService = new GradingPeriodService();

                var grades = gradeService.GetAll()
                    .Where(g => g.StudentId == _studentDetailId.Value)
                    .ToList();

                var periods = gradingPeriodService.GetAll().ToDictionary(p => p.Id, p => p.Name);
                var offerings = _classOfferingService.GetAll().ToDictionary(o => o.Id);
                var subjects = _subjectService.GetAll().ToDictionary(s => s.Id, s => s.Title);

                var gradeRows = grades.Select(g =>
                {
                    var subjectName = "";
                    if (offerings.TryGetValue(g.ClassOfferingId, out var offering))
                    {
                        subjectName = subjects.TryGetValue(offering.SubjectId, out var sName) ? sName : offering.Id.ToString();
                    }

                    return new StudentDetailGradeRow
                    {
                        Subject = subjectName,
                        Period = periods.TryGetValue(g.GradingPeriodId, out var pName) ? pName : "",
                        WrittenWorks = g.WrittenWorks?.ToString("F1") ?? "-",
                        PerformanceTasks = g.PerformanceTasks?.ToString("F1") ?? "-",
                        QuarterlyAssessment = g.QuarterlyAssessment?.ToString("F1") ?? "-",
                        QuarterGrade = g.QuarterGrade?.ToString("F1") ?? "-"
                    };
                }).ToList();

                gridSDGrades.ItemsSource = gradeRows;
            }
            catch (Exception ex)
            {
                gridSDGrades.ItemsSource = null;
                System.Diagnostics.Debug.WriteLine($"Load grades failed: {ex.Message}");
            }
        }

        private void LoadStudentDetailSubjects()
        {
            if (!_studentDetailId.HasValue) return;

            try
            {
                var classStudentService = new ClassStudentService();
                var teacherService = new TeacherService();

                var classStudents = classStudentService.GetAll()
                    .Where(cs => cs.StudentId == _studentDetailId.Value)
                    .ToList();

                var offerings = _classOfferingService.GetAll().ToDictionary(o => o.Id);
                var subjects = _subjectService.GetAll().ToDictionary(s => s.Id, s => s.Title);
                var teachers = teacherService.GetAll().ToDictionary(t => t.Id);
                var enrollments = _enrollmentService.GetAll()
                    .Where(e => e.StudentId == _studentDetailId.Value)
                    .ToDictionary(e => e.Id);

                var subjectRows = classStudents.Select(cs =>
                {
                    var className = "";
                    var subjectName = "";
                    var teacherName = "";

                    if (offerings.TryGetValue(cs.ClassOfferingId, out var offering))
                    {
                        className = $"{offering.Id}";
                        if (subjects.TryGetValue(offering.SubjectId, out var sName))
                        {
                            subjectName = sName;
                            className = sName;
                        }
                        if (offering.TeacherId.HasValue && teachers.TryGetValue(offering.TeacherId.Value, out var teacher))
                        {
                            teacherName = $"{teacher.LastName}, {teacher.FirstName}";
                        }
                    }

                    var enrollmentStatus = "Enrolled";
                    if (enrollments.TryGetValue(cs.EnrollmentId, out var enrollment))
                    {
                        enrollmentStatus = enrollment.Status.ToString();
                    }

                    return new StudentDetailSubjectRow
                    {
                        ClassName = className,
                        SubjectName = subjectName,
                        TeacherName = teacherName,
                        EnrollmentStatus = enrollmentStatus
                    };
                }).ToList();

                gridSDSubjects.ItemsSource = subjectRows;
            }
            catch (Exception ex)
            {
                gridSDSubjects.ItemsSource = null;
                System.Diagnostics.Debug.WriteLine($"Load subjects failed: {ex.Message}");
            }
        }

        private void EnterStudentDetailEditMode()
        {
            _studentDetailEditMode = true;
            SetStudentDetailFieldsEditable(true);
            studentDetailEditBanner.Visibility = Visibility.Visible;
            btnStudentDetailEdit.Visibility = Visibility.Collapsed;
            btnStudentDetailDelete.Visibility = Visibility.Collapsed;
        }

        private void ExitStudentDetailEditMode()
        {
            _studentDetailEditMode = false;
            SetStudentDetailFieldsEditable(false);
            studentDetailEditBanner.Visibility = Visibility.Collapsed;
            studentDetailValidationBanner.Visibility = Visibility.Collapsed;
            btnStudentDetailEdit.Visibility = Visibility.Visible;
            btnStudentDetailDelete.Visibility = Visibility.Visible;
            PopulateStudentDetailFields();
        }

        private void SetStudentDetailFieldsEditable(bool editable)
        {
            txtSDLrn.IsReadOnly = !editable;
            txtSDFirstName.IsReadOnly = !editable;
            txtSDLastName.IsReadOnly = !editable;
            txtSDMiddleName.IsReadOnly = !editable;
            dpSDBirthdate.IsEnabled = editable;
            cboSDSex.IsEnabled = editable;
            txtSDAddress.IsReadOnly = !editable;
            txtSDContactNo.IsReadOnly = !editable;
            txtSDGuardianName.IsReadOnly = !editable;
            txtSDGuardianContact.IsReadOnly = !editable;
            txtSDPreviousSchool.IsReadOnly = !editable;
            cboSDPreferredGrade.IsEnabled = editable;
            cboSDPreferredCurriculum.IsEnabled = editable;
            cboSDStatus.IsEnabled = editable;
            txtSDStudentNumber.IsReadOnly = true; // always read-only
        }

        private void SaveStudentDetail()
        {
            if (_studentDetailRecord == null) return;

            var errors = new List<string>();
            var lrn = txtSDLrn.Text.Trim();
            var firstName = txtSDFirstName.Text.Trim();
            var lastName = txtSDLastName.Text.Trim();

            if (string.IsNullOrWhiteSpace(lrn)) errors.Add("LRN is required.");
            if (string.IsNullOrWhiteSpace(firstName)) errors.Add("First name is required.");
            if (string.IsNullOrWhiteSpace(lastName)) errors.Add("Last name is required.");

            var duplicateLrn = _studentService.GetAll()
                .Any(s => s.Id != _studentDetailRecord.Id && string.Equals(s.Lrn?.Trim(), lrn, StringComparison.OrdinalIgnoreCase));
            if (duplicateLrn) errors.Add("LRN already exists.");

            var birthdate = dpSDBirthdate.SelectedDate?.Date;
            if (birthdate.HasValue)
            {
                var duplicateName = _studentService.GetAll()
                    .Any(s => s.Id != _studentDetailRecord.Id &&
                         string.Equals(s.FirstName?.Trim(), firstName, StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(s.LastName?.Trim(), lastName, StringComparison.OrdinalIgnoreCase) &&
                         Nullable.Equals(s.Birthdate?.Date, birthdate));
                if (duplicateName) errors.Add("A student with the same name and birthdate already exists.");
            }

            if (errors.Count > 0)
            {
                txtStudentDetailValidation.Text = string.Join("\n", errors.Select(e => $"• {e}"));
                studentDetailValidationBanner.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                var oldData = new
                {
                    _studentDetailRecord.Lrn,
                    _studentDetailRecord.FirstName,
                    _studentDetailRecord.LastName,
                    _studentDetailRecord.MiddleName,
                    _studentDetailRecord.Birthdate,
                    _studentDetailRecord.Sex,
                    _studentDetailRecord.Address,
                    _studentDetailRecord.ContactNo,
                    _studentDetailRecord.GuardianName,
                    _studentDetailRecord.GuardianContact,
                    _studentDetailRecord.PreviousSchool,
                    _studentDetailRecord.PreferredGradeLevelId,
                    _studentDetailRecord.PreferredCurriculumId,
                    _studentDetailRecord.Status
                };

                _studentDetailRecord.Lrn = lrn;
                _studentDetailRecord.FirstName = firstName;
                _studentDetailRecord.LastName = lastName;
                _studentDetailRecord.MiddleName = string.IsNullOrWhiteSpace(txtSDMiddleName.Text) ? null : txtSDMiddleName.Text.Trim();
                _studentDetailRecord.Birthdate = birthdate;
                _studentDetailRecord.Age = ComputeAge(birthdate);
                _studentDetailRecord.Sex = cboSDSex.SelectedItem is Sex selectedSex ? selectedSex : null;
                _studentDetailRecord.Address = string.IsNullOrWhiteSpace(txtSDAddress.Text) ? null : txtSDAddress.Text.Trim();
                _studentDetailRecord.ContactNo = string.IsNullOrWhiteSpace(txtSDContactNo.Text) ? null : txtSDContactNo.Text.Trim();
                _studentDetailRecord.GuardianName = string.IsNullOrWhiteSpace(txtSDGuardianName.Text) ? null : txtSDGuardianName.Text.Trim();
                _studentDetailRecord.GuardianContact = txtSDGuardianContact.Text.Trim();
                _studentDetailRecord.PreviousSchool = string.IsNullOrWhiteSpace(txtSDPreviousSchool.Text) ? null : txtSDPreviousSchool.Text.Trim();
                _studentDetailRecord.PreferredGradeLevelId = cboSDPreferredGrade.SelectedValue is long gradeId && gradeId > 0 ? gradeId : null;
                _studentDetailRecord.PreferredCurriculumId = cboSDPreferredCurriculum.SelectedValue is long currId && currId > 0 ? currId : null;
                _studentDetailRecord.Status = cboSDStatus.SelectedItem is UserStatus status ? status : _studentDetailRecord.Status;
                _studentDetailRecord.UpdatedAt = DateTime.UtcNow;

                _studentService.Update(_studentDetailRecord);
                AuditTrailService.Log("UPDATE", "students", _studentDetailRecord.Id, oldData, _studentDetailRecord);

                var syncResult = _studentAccountService.SyncStudentAccount(_studentDetailRecord.Id);
                if (!syncResult.Success)
                {
                    MessageBox.Show(syncResult.Message, "Student Account", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                ExitStudentDetailEditMode();
                LoadStudentDetailData();
            }
            catch (DomainValidationException ex)
            {
                txtStudentDetailValidation.Text = $"• {ex.Message}";
                studentDetailValidationBanner.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteStudentDetail()
        {
            if (_studentDetailRecord == null) return;

            var dependents = new List<string>();
            // Use the shared enrollment cache instead of a full GetAll() scan
            var enrollmentCount = _cachedEnrollments.Count(e => e.StudentId == _studentDetailId!.Value);
            var gradeService = new StudentGradeService();
            var classStudentService = new ClassStudentService();
            var requirementService = new StudentRequirementService();
            var gradeCount = gradeService.GetAll().Count(g => g.StudentId == _studentDetailId!.Value);
            var classCount = classStudentService.GetAll().Count(cs => cs.StudentId == _studentDetailId!.Value);
            var reqCount = requirementService.GetAll().Count(r => r.StudentId == _studentDetailId!.Value);

            if (enrollmentCount > 0) dependents.Add($"{enrollmentCount} enrollment(s)");
            if (gradeCount > 0) dependents.Add($"{gradeCount} grade record(s)");
            if (classCount > 0) dependents.Add($"{classCount} class assignment(s)");
            if (reqCount > 0) dependents.Add($"{reqCount} requirement(s)");

            var message = "Are you sure you want to delete this student?";
            if (dependents.Count > 0)
            {
                message += $"\n\nWARNING: This student has the following dependent records that will also be removed:\n• {string.Join("\n• ", dependents)}";
            }

            var confirm = MessageBox.Show(message, "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var user = _userService.GetById(_studentDetailRecord.UserId);
                AuditTrailService.Log("DELETE", "students", _studentDetailRecord.Id, _studentDetailRecord, null);
                _studentService.Delete(_studentDetailRecord.Id);

                if (user != null)
                {
                    AuditTrailService.Log("DELETE", "users", user.Id, user, null);
                    _userService.Delete(user.Id);
                }

                _studentDetailId = null;
                _studentDetailRecord = null;
                NavigateMainTab(0); // Go back to dashboard
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenStudentDetailRequirements()
        {
            if (!_studentDetailId.HasValue) return;
            new StudentRequirementsWindow(_studentDetailId.Value) { Owner = this }.ShowDialog();
            LoadStudentDetailRequirements();
        }

        public class StudentDetailRequirementRow
        {
            public string Name { get; set; } = "";
            public string Status { get; set; } = "";
            public string SubmittedDate { get; set; } = "";
            public string Notes { get; set; } = "";
        }

        public class StudentDetailGradeRow
        {
            public string Subject { get; set; } = "";
            public string Period { get; set; } = "";
            public string WrittenWorks { get; set; } = "";
            public string PerformanceTasks { get; set; } = "";
            public string QuarterlyAssessment { get; set; } = "";
            public string QuarterGrade { get; set; } = "";
        }

        public class StudentDetailSubjectRow
        {
            public string ClassName { get; set; } = "";
            public string SubjectName { get; set; } = "";
            public string TeacherName { get; set; } = "";
            public string EnrollmentStatus { get; set; } = "";
        }
    }
}

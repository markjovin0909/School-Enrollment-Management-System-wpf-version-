using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.Views;

namespace School_Management_System
{
    public partial class MainWindow
    {
        private const int TeacherDetailsTabIndex = 11;

        private static readonly string[] TDAdvisoryStatuses = { "UNASSIGNED", "ASSIGNED" };
        private static readonly string[] TDEmploymentStatuses = { "REGULAR", "PROBATIONARY", "PART_TIME", "CONTRACTUAL" };

        private long? _teacherDetailId;
        private Teacher? _teacherDetailRecord;

        private void InitializeTeacherDetailsTab()
        {
            cboTDStatus.ItemsSource = Enum.GetValues(typeof(UserStatus));
            cboTDAdvisoryStatus.ItemsSource = TDAdvisoryStatuses;
            cboTDEmploymentStatus.ItemsSource = TDEmploymentStatuses;

            btnTDEdit.Click += (_, _) => OpenTeacherDetailEditDialog();
            btnTDDelete.Click += (_, _) => DeleteTeacherDetail();
            gridTDSections.MouseDoubleClick += GridTDSections_MouseDoubleClick;

            SetTeacherDetailFieldsEditable(false);
            tdEditBanner.Visibility = Visibility.Collapsed;
            tdValidationBanner.Visibility = Visibility.Collapsed;
        }

        private void OpenTeacherSearchModal()
        {
            try
            {
                var modal = new TeacherSearchModal { Owner = this };
                if (modal.ShowDialog() == true && modal.SelectedTeacherId.HasValue)
                {
                    NavigateToTeacherDetails(modal.SelectedTeacherId.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open teacher search: {ex.Message}", "Teachers", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToTeacherDetails(long teacherId)
        {
            _teacherDetailId = teacherId;
            LoadTeacherDetailData();
            NavigateMainTab(TeacherDetailsTabIndex);
        }

        private void LoadTeacherDetailData()
        {
            if (!_teacherDetailId.HasValue) return;

            _teacherDetailRecord = _teacherService.GetById(_teacherDetailId.Value);
            if (_teacherDetailRecord == null)
            {
                MessageBox.Show("Teacher not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigateMainTab(0);
                return;
            }

            var fullName = $"{_teacherDetailRecord.LastName}, {_teacherDetailRecord.FirstName}{(string.IsNullOrWhiteSpace(_teacherDetailRecord.MiddleName) ? "" : $" {_teacherDetailRecord.MiddleName}")}";
            sectionTDHeader.Title = fullName;
            sectionTDHeader.Subtitle = $"Employee No: {_teacherDetailRecord.EmployeeNo ?? "N/A"}  |  {_teacherDetailRecord.Specialization ?? "No specialization"}";

            SetTeacherDetailFieldsEditable(false);
            tdEditBanner.Visibility = Visibility.Collapsed;
            tdValidationBanner.Visibility = Visibility.Collapsed;
            PopulateTeacherDetailFields();
            LoadTeacherDetailSubjects();
            LoadTeacherDetailSections();
        }

        private void PopulateTeacherDetailFields()
        {
            if (_teacherDetailRecord == null) return;

            txtTDEmployeeNo.Text = _teacherDetailRecord.EmployeeNo ?? "";
            txtTDSpecialization.Text = _teacherDetailRecord.Specialization ?? "";
            txtTDFirstName.Text = _teacherDetailRecord.FirstName;
            txtTDLastName.Text = _teacherDetailRecord.LastName;
            txtTDMiddleName.Text = _teacherDetailRecord.MiddleName ?? "";
            txtTDEmail.Text = _teacherDetailRecord.Email ?? "";
            txtTDContactNo.Text = _teacherDetailRecord.ContactNo ?? "";
            dpTDHireDate.SelectedDate = _teacherDetailRecord.HireDate?.Date;
            cboTDStatus.SelectedItem = _teacherDetailRecord.Status;
            cboTDAdvisoryStatus.SelectedItem = _teacherDetailRecord.AdvisoryAssignmentStatus ?? "UNASSIGNED";
            cboTDEmploymentStatus.SelectedItem = _teacherDetailRecord.EmploymentStatus ?? "REGULAR";
        }

        private void LoadTeacherDetailSubjects()
        {
            if (!_teacherDetailId.HasValue) return;

            try
            {
                var offerings = _classOfferingService.GetAll()
                    .Where(o => o.TeacherId == _teacherDetailId.Value)
                    .ToList();

                var subjects = _subjectService.GetAll().ToDictionary(s => s.Id);
                var sections = _sectionService.GetAll().ToDictionary(s => s.Id);
                var gradeLevels = _gradeLevelService.GetAll().ToDictionary(g => g.Id);
                var schoolYears = _schoolYearService.GetAll().ToDictionary(sy => sy.Id);

                var rows = offerings.Select(o =>
                {
                    var subjectName = subjects.TryGetValue(o.SubjectId, out var subj) ? subj.Title : "Unknown";
                    var sectionName = sections.TryGetValue(o.SectionId, out var sec) ? sec.Name : "Unknown";
                    var gradeLevel = sec != null && gradeLevels.TryGetValue(sec.GradeLevelId, out var gl) ? (gl.Code ?? gl.Name) : "";
                    var schoolYear = schoolYears.TryGetValue(o.SchoolYearId, out var sy) ? sy.Name : "";

                    return new TDSubjectRow
                    {
                        SubjectName = subjectName,
                        SectionName = sectionName,
                        GradeLevel = gradeLevel,
                        SchoolYear = schoolYear,
                        Status = o.Status.ToString()
                    };
                }).ToList();

                gridTDSubjects.ItemsSource = rows;
            }
            catch (Exception ex)
            {
                gridTDSubjects.ItemsSource = null;
                System.Diagnostics.Debug.WriteLine($"Load teacher subjects failed: {ex.Message}");
            }
        }

        private void LoadTeacherDetailSections()
        {
            if (!_teacherDetailId.HasValue) return;

            try
            {
                var offerings = _classOfferingService.GetAll()
                    .Where(o => o.TeacherId == _teacherDetailId.Value)
                    .ToList();

                var sections = _sectionService.GetAll().ToDictionary(s => s.Id);
                var gradeLevels = _gradeLevelService.GetAll().ToDictionary(g => g.Id);
                var schoolYears = _schoolYearService.GetAll().ToDictionary(sy => sy.Id);
                var classStudentService = new ClassStudentService();
                var allClassStudents = classStudentService.GetAll().Where(cs => cs.Status == ClassStudentStatus.ACTIVE).ToList();

                var sectionGroups = offerings
                    .GroupBy(o => o.SectionId)
                    .Select(g =>
                    {
                        var sectionId = g.Key;
                        var section = sections.TryGetValue(sectionId, out var sec) ? sec : null;
                        var gradeLevel = section != null && gradeLevels.TryGetValue(section.GradeLevelId, out var gl) ? (gl.Code ?? gl.Name) : "";
                        var schoolYear = schoolYears.TryGetValue(g.First().SchoolYearId, out var sy) ? sy.Name : "";
                        var offeringIds = g.Select(o => o.Id).ToHashSet();
                        var studentCount = allClassStudents.Where(cs => offeringIds.Contains(cs.ClassOfferingId)).Select(cs => cs.StudentId).Distinct().Count();

                        return new TDSectionRow
                        {
                            SectionId = sectionId,
                            SectionName = section?.Name ?? "Unknown",
                            GradeLevel = gradeLevel,
                            SchoolYear = schoolYear,
                            StudentCount = studentCount
                        };
                    })
                    .OrderBy(r => r.SectionName)
                    .ToList();

                gridTDSections.ItemsSource = sectionGroups;
            }
            catch (Exception ex)
            {
                gridTDSections.ItemsSource = null;
                System.Diagnostics.Debug.WriteLine($"Load teacher sections failed: {ex.Message}");
            }
        }

        private void GridTDSections_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (gridTDSections.SelectedItem is TDSectionRow row)
            {
                new SectionStudentsModal(row.SectionId, row.SectionName) { Owner = this }.ShowDialog();
            }
        }

        private void SetTeacherDetailFieldsEditable(bool editable)
        {
            txtTDEmployeeNo.IsReadOnly = !editable;
            txtTDSpecialization.IsReadOnly = !editable;
            txtTDFirstName.IsReadOnly = !editable;
            txtTDLastName.IsReadOnly = !editable;
            txtTDMiddleName.IsReadOnly = !editable;
            txtTDEmail.IsReadOnly = !editable;
            txtTDContactNo.IsReadOnly = !editable;
            dpTDHireDate.IsEnabled = editable;
            cboTDStatus.IsEnabled = editable;
            cboTDAdvisoryStatus.IsEnabled = editable;
            cboTDEmploymentStatus.IsEnabled = editable;
        }

        private void OpenTeacherDetailEditDialog()
        {
            if (!_teacherDetailId.HasValue)
            {
                return;
            }

            try
            {
                var dialog = new TeacherCreateWindow(_teacherDetailId.Value) { Owner = this };
                if (dialog.ShowDialog() == true && dialog.SavedTeacherId.HasValue)
                {
                    _teacherDetailId = dialog.SavedTeacherId.Value;
                    LoadTeacherDetailData();
                }
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError("Open edit teacher dialog failed.", ex, "Teacher Details", this);
            }
        }

        private void DeleteTeacherDetail()
        {
            if (_teacherDetailRecord == null) return;

            if (!AppFeedbackService.Confirm("Are you sure you want to delete this teacher?", "Confirm Delete", this)) return;

            try
            {
                var user = _userService.GetById(_teacherDetailRecord.UserId);
                AuditTrailService.Log("DELETE", "teachers", _teacherDetailRecord.Id, _teacherDetailRecord, null);
                _teacherService.Delete(_teacherDetailRecord.Id);

                if (user != null)
                {
                    AuditTrailService.Log("DELETE", "users", user.Id, user, null);
                    _userService.Delete(user.Id);
                }

                _teacherDetailId = null;
                _teacherDetailRecord = null;
                NavigateMainTab(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Delete failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public class TDSubjectRow
        {
            public string SubjectName { get; set; } = "";
            public string SectionName { get; set; } = "";
            public string GradeLevel { get; set; } = "";
            public string SchoolYear { get; set; } = "";
            public string Status { get; set; } = "";
        }

        public class TDSectionRow
        {
            public long SectionId { get; set; }
            public string SectionName { get; set; } = "";
            public string GradeLevel { get; set; } = "";
            public string SchoolYear { get; set; } = "";
            public int StudentCount { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class EnrollmentModal : Window
    {
        private readonly StudentService _studentService = new();
        private readonly SchoolYearService _schoolYearService = new();
        private readonly CurriculumService _curriculumService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly SectionService _sectionService = new();
        private readonly ClassOfferingService _classOfferingService = new();
        private readonly SubjectService _subjectService = new();
        private readonly TeacherService _teacherService = new();
        private readonly EnrollmentService _enrollmentService = new();

        private List<StudentItem> _allStudents = new();
        private long? _selectedStudentId;

        public long? CreatedEnrollmentId { get; private set; }

        public EnrollmentModal()
        {
            InitializeComponent();

            btnClose.Click += (_, _) => Close();
            txtStudentSearch.TextChanged += (_, _) => ApplyStudentFilter();
            lstStudents.MouseDoubleClick += LstStudents_MouseDoubleClick;
            btnBack.Click += (_, _) => GoToStep1();
            btnEnroll.Click += (_, _) => SubmitEnrollment();

            cboSchoolYear.SelectionChanged += (_, _) => LoadSections();
            cboGradeLevel.SelectionChanged += (_, _) => LoadSections();
            cboSection.SelectionChanged += (_, _) => LoadOfferings();

            LoadStudents();
        }

        private void LoadStudents()
        {
            _allStudents = _studentService.GetAll()
                .OrderBy(s => s.LastName)
                .ThenBy(s => s.FirstName)
                .Select(s => new StudentItem
                {
                    Id = s.Id,
                    FullName = $"{s.LastName}, {s.FirstName}{(string.IsNullOrWhiteSpace(s.MiddleName) ? "" : $" {s.MiddleName}")}",
                    Lrn = s.Lrn ?? string.Empty,
                    StudentNumber = s.StudentNumber ?? string.Empty
                })
                .ToList();

            ApplyStudentFilter();
        }

        private void ApplyStudentFilter()
        {
            var term = (txtStudentSearch.Text ?? string.Empty).Trim();
            var filtered = string.IsNullOrWhiteSpace(term)
                ? _allStudents
                : _allStudents.Where(s =>
                    s.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    s.Lrn.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    s.StudentNumber.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

            lstStudents.ItemsSource = filtered;
            txtStep1Info.Text = $"{filtered.Count} of {_allStudents.Count} students";
        }

        private void LstStudents_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstStudents.SelectedItem is not StudentItem selected)
            {
                return;
            }

            _selectedStudentId = selected.Id;
            txtSelectedStudent.Text = $"Enrolling: {selected.FullName} (LRN: {selected.Lrn})";
            GoToStep2();
        }

        private void GoToStep1()
        {
            pnlStep1.Visibility = Visibility.Visible;
            pnlStep2.Visibility = Visibility.Collapsed;
        }

        private void GoToStep2()
        {
            pnlStep1.Visibility = Visibility.Collapsed;
            pnlStep2.Visibility = Visibility.Visible;
            LoadStep2Lookups();
        }

        private void LoadStep2Lookups()
        {
            var schoolYears = _schoolYearService.GetAll()
                .Where(sy => !sy.IsArchived)
                .OrderByDescending(sy => sy.StartDate ?? DateTime.MinValue)
                .Select(sy => new LookupItem(sy.Id, sy.Name))
                .ToList();
            cboSchoolYear.DisplayMemberPath = nameof(LookupItem.Label);
            cboSchoolYear.SelectedValuePath = nameof(LookupItem.Id);
            cboSchoolYear.ItemsSource = schoolYears;
            var activeSchoolYearId = SchoolYearSelectionHelper.ResolveActiveId(
                _schoolYearService.GetAll().Where(sy => !sy.IsArchived),
                _schoolYearService);
            if (activeSchoolYearId.HasValue && schoolYears.Any(x => x.Id == activeSchoolYearId.Value))
            {
                cboSchoolYear.SelectedValue = activeSchoolYearId.Value;
            }
            else
            {
                cboSchoolYear.SelectedIndex = schoolYears.Count > 0 ? 0 : -1;
            }

            var curricula = _curriculumService.GetAll()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new LookupItem(c.Id, c.Name))
                .ToList();
            cboCurriculum.DisplayMemberPath = nameof(LookupItem.Label);
            cboCurriculum.SelectedValuePath = nameof(LookupItem.Id);
            cboCurriculum.ItemsSource = curricula;
            cboCurriculum.SelectedIndex = curricula.Count > 0 ? 0 : -1;

            var settingService = new SchoolSettingService();
            var orderedGrades = settingService.OrderGradeLevelsByDefaultScope(_gradeLevelService.GetAll());
            var gradeLevels = orderedGrades
                .Select(g => new LookupItem(g.Id, string.IsNullOrWhiteSpace(g.Code) ? g.Name : $"{g.Code} - {g.Name}"))
                .ToList();
            cboGradeLevel.DisplayMemberPath = nameof(LookupItem.Label);
            cboGradeLevel.SelectedValuePath = nameof(LookupItem.Id);
            cboGradeLevel.ItemsSource = gradeLevels;
            var preferredGradeId = settingService.GetPrimaryDefaultGradeLevelId();
            if (preferredGradeId.HasValue && gradeLevels.Any(x => x.Id == preferredGradeId.Value))
            {
                cboGradeLevel.SelectedValue = preferredGradeId.Value;
            }
            else
            {
                cboGradeLevel.SelectedIndex = gradeLevels.Count > 0 ? 0 : -1;
            }

            LoadSections();
        }

        private void LoadSections()
        {
            var schoolYearId = cboSchoolYear.SelectedValue is long sy ? sy : 0L;
            var gradeLevelId = cboGradeLevel.SelectedValue is long gl ? gl : 0L;

            var sections = _sectionService.GetAll()
                .Where(s => !s.IsArchived &&
                    (schoolYearId == 0 || s.SchoolYearId == schoolYearId) &&
                    (gradeLevelId == 0 || s.GradeLevelId == gradeLevelId))
                .OrderBy(s => s.Name)
                .Select(s => new LookupItem(s.Id, s.Name))
                .ToList();

            cboSection.DisplayMemberPath = nameof(LookupItem.Label);
            cboSection.SelectedValuePath = nameof(LookupItem.Id);
            cboSection.ItemsSource = sections;
            cboSection.SelectedIndex = sections.Count > 0 ? 0 : -1;

            LoadOfferings();
        }

        private void LoadOfferings()
        {
            var sectionId = cboSection.SelectedValue is long sec ? sec : 0L;
            if (sectionId == 0)
            {
                gridOfferings.ItemsSource = null;
                return;
            }

            var subjects = _subjectService.GetAll().ToDictionary(s => s.Id);
            var teachers = _teacherService.GetAll().ToDictionary(t => t.Id);

            var offerings = _classOfferingService.GetAll()
                .Where(o => o.SectionId == sectionId && o.Status != ClassOfferingStatus.ARCHIVED)
                .Select(o =>
                {
                    var subjectName = subjects.TryGetValue(o.SubjectId, out var subj) ? subj.Title : "Unknown";
                    var teacherName = o.TeacherId.HasValue && teachers.TryGetValue(o.TeacherId.Value, out var teacher)
                        ? $"{teacher.LastName}, {teacher.FirstName}"
                        : "Unassigned";
                    return new OfferingRow
                    {
                        SubjectName = subjectName,
                        TeacherName = teacherName,
                        Status = o.Status.ToString()
                    };
                })
                .ToList();

            gridOfferings.ItemsSource = offerings;
        }

        private void SubmitEnrollment()
        {
            if (!_selectedStudentId.HasValue)
            {
                AppFeedbackService.ShowWarning("No student selected.", "Enrollment", this);
                return;
            }

            var schoolYearId = cboSchoolYear.SelectedValue is long sy ? sy : 0L;
            var sectionId = cboSection.SelectedValue is long sec ? sec : 0L;
            var curriculumId = cboCurriculum.SelectedValue is long cur ? cur : 0L;

            if (schoolYearId == 0 || sectionId == 0 || curriculumId == 0)
            {
                AppFeedbackService.ShowWarning("Please select school year, section, and curriculum.", "Enrollment", this);
                return;
            }

            try
            {
                var draft = new EnrollmentDraft
                {
                    SchoolYearId = schoolYearId,
                    StudentId = _selectedStudentId.Value,
                    SectionId = sectionId,
                    CurriculumId = curriculumId,
                    EnrollmentType = "NEW"
                };

                var validationResult = _enrollmentService.BuildValidationSummary(draft);
                if (validationResult.Success && validationResult.Data != null && !validationResult.Data.CanSubmit)
                {
                    AppFeedbackService.ShowWarning(
                        validationResult.Data.ToDisplayText(),
                        "Enrollment Cannot Be Submitted",
                        this);
                    return;
                }

                var result = _enrollmentService.SubmitEnrollmentRequest(draft);
                if (!result.Success || result.Data == null)
                {
                    AppFeedbackService.ShowDetailedWarning(
                        result.Message ?? "Enrollment failed.",
                        result.Errors,
                        "Enrollment",
                        this);
                    return;
                }

                CreatedEnrollmentId = result.Data.Id;
                AppFeedbackService.ShowSuccess(
                    result.Message ?? "Enrollment submitted successfully.",
                    "Enrollment",
                    this);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError($"Enrollment failed: {ex.Message}", "Enrollment", this);
            }
        }

        public class StudentItem
        {
            public long Id { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Lrn { get; set; } = string.Empty;
            public string StudentNumber { get; set; } = string.Empty;
        }

        private sealed class LookupItem
        {
            public LookupItem(long id, string label)
            {
                Id = id;
                Label = label;
            }

            public long Id { get; }
            public string Label { get; }
        }

        public class OfferingRow
        {
            public string SubjectName { get; set; } = string.Empty;
            public string TeacherName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }
    }
}

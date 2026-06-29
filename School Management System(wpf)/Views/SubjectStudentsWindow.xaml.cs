using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class SubjectStudentsWindow : Window
    {
        private readonly ClassOfferingService _classOfferingService = new();
        private readonly ClassStudentService _classStudentService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly SchoolYearService _schoolYearService = new();
        private readonly SectionService _sectionService = new();
        private readonly StudentService _studentService = new();
        private readonly SubjectService _subjectService = new();
        private readonly bool _hostedInline;

        private List<ClassOffering> _offerings = new();
        private List<GradeLevel> _gradeLevels = new();
        private List<SchoolYear> _schoolYears = new();
        private List<Section> _sections = new();
        private List<Subject> _subjects = new();
        private bool _suppressEvents;

        public SubjectStudentsWindow(bool hostedInline = false)
        {
            _hostedInline = hostedInline;
            InitializeComponent();

            cboSchoolYear.SelectionChanged += FiltersChanged;
            cboGradeLevel.SelectionChanged += FiltersChanged;
            cboSubject.SelectionChanged += FiltersChanged;
            cboOffering.SelectionChanged += (_, _) =>
            {
                if (!_suppressEvents)
                {
                    LoadStudents();
                }
            };
            btnRefresh.Click += (_, _) =>
            {
                LoadLookups();
                LoadStudents();
            };

            if (_hostedInline)
            {
                btnClose.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnClose.Click += (_, _) => Close();
            }

            LoadLookups();
            LoadStudents();
        }

        private void LoadLookups()
        {
            _suppressEvents = true;

            _schoolYears = _schoolYearService.GetAll()
                .Where(x => !x.IsArchived)
                .OrderByDescending(x => x.Name)
                .ToList();
            _subjects = _subjectService.GetAll()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Title)
                .ToList();
            _offerings = _classOfferingService.GetAll()
                .Where(x => x.Status != ClassOfferingStatus.ARCHIVED)
                .ToList();
            _sections = _sectionService.GetAll().ToList();
            _gradeLevels = _gradeLevelService.GetAll().ToList();

            cboSchoolYear.ItemsSource = _schoolYears;
            cboSchoolYear.SelectedIndex = _schoolYears.Count > 0 ? 0 : -1;

            cboGradeLevel.ItemsSource = _gradeLevels.OrderBy(x => x.Code).ToList();
            cboGradeLevel.SelectedIndex = _gradeLevels.Count > 0 ? 0 : -1;

            LoadSubjectsCombo();
            LoadOfferingsCombo();

            _suppressEvents = false;
        }

        private void FiltersChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_suppressEvents)
            {
                return;
            }

            if (ReferenceEquals(sender, cboGradeLevel))
            {
                LoadSubjectsCombo();
            }

            LoadOfferingsCombo();
            LoadStudents();
        }

        private void LoadSubjectsCombo()
        {
            var gradeLevelId = cboGradeLevel.SelectedValue is long grade ? grade : (long?)null;

            var options = _subjects
                .Where(s => !gradeLevelId.HasValue || s.GradeLevelId == gradeLevelId.Value)
                .Select(s => new SubjectOption(s.Id, $"{s.Code} - {s.Title}"))
                .OrderBy(s => s.Label)
                .ToList();

            _suppressEvents = true;
            cboSubject.ItemsSource = options;
            cboSubject.SelectedIndex = options.Count > 0 ? 0 : -1;
            _suppressEvents = false;
        }

        private void LoadOfferingsCombo()
        {
            var schoolYearId = cboSchoolYear.SelectedValue is long sy ? sy : (long?)null;
            var gradeLevelId = cboGradeLevel.SelectedValue is long grade ? grade : (long?)null;
            var subjectId = cboSubject.SelectedValue is long subject ? subject : (long?)null;

            var options = _offerings
                .Where(o => (!schoolYearId.HasValue || o.SchoolYearId == schoolYearId.Value) &&
                            (!gradeLevelId.HasValue || _sections.Any(s => s.Id == o.SectionId && s.GradeLevelId == gradeLevelId.Value)) &&
                            (!subjectId.HasValue || o.SubjectId == subjectId.Value))
                .Select(o =>
                {
                    var section = _sections.FirstOrDefault(s => s.Id == o.SectionId);
                    var grade = section == null
                        ? string.Empty
                        : _gradeLevels.FirstOrDefault(g => g.Id == section.GradeLevelId)?.Code ?? string.Empty;
                    var subjectName = _subjects.FirstOrDefault(s => s.Id == o.SubjectId)?.Title ?? "Subject";
                    var sectionName = section?.Name ?? "Unknown Section";
                    return new OfferingOption(o.Id, $"{grade} - {sectionName} | {subjectName} | {o.Status}");
                })
                .OrderBy(o => o.Label)
                .ToList();

            _suppressEvents = true;
            cboOffering.ItemsSource = options;
            cboOffering.SelectedIndex = options.Count > 0 ? 0 : -1;
            _suppressEvents = false;
        }

        private void LoadStudents()
        {
            if (cboOffering.SelectedValue is not long offeringId)
            {
                gridStudents.ItemsSource = null;
                txtSummary.Text = "No subject offering found for the selected filters.";
                return;
            }

            try
            {
                var classStudents = _classStudentService.GetAll()
                    .Where(cs => cs.ClassOfferingId == offeringId && cs.Status == ClassStudentStatus.ACTIVE)
                    .ToList();

                var studentIds = classStudents
                    .Select(cs => cs.StudentId)
                    .Distinct()
                    .ToList();

                var students = _studentService.GetAll()
                    .Where(s => studentIds.Contains(s.Id))
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .Select(s =>
                    {
                        var classStudent = classStudents.First(cs => cs.StudentId == s.Id);
                        return new StudentRow
                        {
                            StudentNumber = s.StudentNumber ?? string.Empty,
                            Lrn = s.Lrn ?? string.Empty,
                            FullName = $"{s.LastName}, {s.FirstName}{(string.IsNullOrWhiteSpace(s.MiddleName) ? string.Empty : $" {s.MiddleName}")}",
                            Status = s.Status.ToString(),
                            ClassStatus = classStudent.Status.ToString()
                        };
                    })
                    .ToList();

                gridStudents.ItemsSource = students;
                txtSummary.Text = students.Count == 0
                    ? "No active students are currently linked to this subject offering."
                    : $"{students.Count} student(s) found in this subject offering.";
            }
            catch (Exception ex)
            {
                gridStudents.ItemsSource = null;
                txtSummary.Text = $"Failed to load students: {ex.Message}";
            }
        }

        private sealed class SubjectOption
        {
            public SubjectOption(long id, string label)
            {
                Id = id;
                Label = label;
            }

            public long Id { get; }
            public string Label { get; }
        }

        private sealed class OfferingOption
        {
            public OfferingOption(long id, string label)
            {
                Id = id;
                Label = label;
            }

            public long Id { get; }
            public string Label { get; }
        }

        public class StudentRow
        {
            public string StudentNumber { get; set; } = string.Empty;
            public string Lrn { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string ClassStatus { get; set; } = string.Empty;
        }
    }
}

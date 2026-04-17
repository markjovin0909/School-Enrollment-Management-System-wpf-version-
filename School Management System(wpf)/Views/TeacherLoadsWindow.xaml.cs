using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class TeacherLoadsWindow : Window
    {
        private sealed class TeacherLoadDetails
        {
            public long TeacherId { get; set; }
            public string TeacherName { get; set; } = "-";
            public string EmployeeNo { get; set; } = "-";
            public string Status { get; set; } = "-";
            public int LoadCount { get; set; }
            public List<string> Advisories { get; set; } = new();
            public List<string> HandledSubjects { get; set; } = new();
        }

        private sealed class SchoolYearFilterItem
        {
            public SchoolYearFilterItem(long id, string name)
            {
                Id = id;
                Name = name;
            }

            public long Id { get; }
            public string Name { get; }
        }

        private readonly TeacherService _teacherService = new();
        private readonly SchoolYearService _schoolYearService = new();
        private readonly SectionService _sectionService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly ClassOfferingService _classOfferingService = new();
        private readonly SubjectService _subjectService = new();
        private readonly bool _hostedInline;

        private Dictionary<long, TeacherLoadDetails> _detailsByTeacherId = new();
        private List<Teacher> _teachers = new();
        private List<SchoolYear> _schoolYears = new();
        private List<Section> _sections = new();
        private List<GradeLevel> _gradeLevels = new();
        private List<ClassOffering> _offerings = new();
        private Dictionary<long, Subject> _subjectLookup = new();

        public TeacherLoadsWindow(bool hostedInline = false)
        {
            _hostedInline = hostedInline;
            InitializeComponent();

            gridTeacherLoads.AutoGeneratingColumn += (_, e) =>
            {
                if (string.Equals(e.PropertyName, "TeacherId", StringComparison.OrdinalIgnoreCase))
                {
                    e.Cancel = true;
                }
            };

            txtSearch.TextChanged += (_, _) => LoadGrid();
            cboSchoolYear.SelectionChanged += (_, _) => LoadGrid();
            cboTeacherStatus.SelectionChanged += (_, _) => LoadGrid();
            gridTeacherLoads.SelectionChanged += GridTeacherLoads_SelectionChanged;
            btnRefresh.Click += (_, _) =>
            {
                LoadLookups();
                LoadGrid();
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
            LoadGrid();
        }

        private void LoadLookups()
        {
            _teachers = _teacherService.GetAll().ToList();
            _schoolYears = _schoolYearService.GetAll().Where(x => !x.IsArchived).OrderByDescending(x => x.Name).ToList();
            _sections = _sectionService.GetAll().Where(x => !x.IsArchived).ToList();
            _gradeLevels = _gradeLevelService.GetAll().ToList();
            _offerings = _classOfferingService.GetAll().ToList();
            _subjectLookup = _subjectService.GetAll()
                .GroupBy(s => s.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var schoolYearFilters = new List<SchoolYearFilterItem> { new(0, "All School Years") };
            schoolYearFilters.AddRange(_schoolYears.Select(x => new SchoolYearFilterItem(x.Id, x.Name)));
            cboSchoolYear.DisplayMemberPath = nameof(SchoolYearFilterItem.Name);
            cboSchoolYear.SelectedValuePath = nameof(SchoolYearFilterItem.Id);
            cboSchoolYear.ItemsSource = schoolYearFilters;
            cboSchoolYear.SelectedIndex = 0;

            cboTeacherStatus.ItemsSource = new[]
            {
                "ALL",
                UserStatus.ACTIVE.ToString(),
                UserStatus.INACTIVE.ToString(),
                UserStatus.LOCKED.ToString()
            };
            cboTeacherStatus.SelectedIndex = 0;
        }

        private void LoadGrid()
        {
            var search = (txtSearch.Text ?? string.Empty).Trim();
            var selectedStatus = cboTeacherStatus.SelectedItem?.ToString() ?? "ALL";
            var schoolYearId = cboSchoolYear.SelectedValue is long id && id > 0 ? id : (long?)null;
            _detailsByTeacherId = new Dictionary<long, TeacherLoadDetails>();

            var teachers = _teachers.AsEnumerable();
            if (!string.Equals(selectedStatus, "ALL", StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<UserStatus>(selectedStatus, out var parsedStatus))
            {
                teachers = teachers.Where(t => t.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                teachers = teachers.Where(t =>
                    (t.FirstName ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.LastName ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.EmployeeNo ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var gradeLookup = _gradeLevels
                .GroupBy(g => g.Id)
                .ToDictionary(g => g.Key, g => g.First().Code);

            var table = new DataTable();
            table.Columns.Add("TeacherId", typeof(long));
            table.Columns.Add("Teacher");
            table.Columns.Add("Employee No");
            table.Columns.Add("Status");
            table.Columns.Add("Advisory");
            table.Columns.Add("Handled Subjects");
            table.Columns.Add("Sections Handled");
            table.Columns.Add("Load Count", typeof(int));

            foreach (var teacher in teachers.OrderBy(t => t.LastName).ThenBy(t => t.FirstName))
            {
                var advisories = _sections
                    .Where(s => s.AdviserTeacherId == teacher.Id && (!schoolYearId.HasValue || s.SchoolYearId == schoolYearId.Value))
                    .Select(s =>
                    {
                        var gradeCode = gradeLookup.TryGetValue(s.GradeLevelId, out var code) ? code : "N/A";
                        return $"{gradeCode}-{s.Name}";
                    })
                    .ToList();

                var assigned = _offerings
                    .Where(o => o.TeacherId == teacher.Id && (!schoolYearId.HasValue || o.SchoolYearId == schoolYearId.Value))
                    .ToList();

                var subjects = assigned
                    .Select(o => _subjectLookup.TryGetValue(o.SubjectId, out var subject) ? subject.Title : "Unknown Subject")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                var handledSections = assigned
                    .Select(o => _sections.FirstOrDefault(s => s.Id == o.SectionId)?.Name ?? string.Empty)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                _detailsByTeacherId[teacher.Id] = new TeacherLoadDetails
                {
                    TeacherId = teacher.Id,
                    TeacherName = $"{teacher.LastName}, {teacher.FirstName}",
                    EmployeeNo = string.IsNullOrWhiteSpace(teacher.EmployeeNo) ? "-" : teacher.EmployeeNo,
                    Status = teacher.Status.ToString(),
                    LoadCount = assigned.Count,
                    Advisories = advisories,
                    HandledSubjects = subjects
                };

                table.Rows.Add(
                    teacher.Id,
                    $"{teacher.LastName}, {teacher.FirstName}",
                    teacher.EmployeeNo ?? "-",
                    teacher.Status.ToString(),
                    advisories.Count > 0 ? string.Join(", ", advisories) : "-",
                    subjects.Count > 0 ? string.Join(", ", subjects) : "-",
                    handledSections.Count > 0 ? string.Join(", ", handledSections) : "-",
                    assigned.Count);
            }

            gridTeacherLoads.ItemsSource = table.DefaultView;
            txtSummary.Text = $"Teachers Displayed: {table.Rows.Count}";
            gridTeacherLoads.SelectedItem = null;
            ClearDetails();
        }

        private void GridTeacherLoads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridTeacherLoads.SelectedItem is not DataRowView row ||
                row.Row.Field<long?>("TeacherId") is not long teacherId ||
                !_detailsByTeacherId.TryGetValue(teacherId, out var detail))
            {
                ClearDetails();
                return;
            }

            txtSelectHint.Text = "Selected Teacher";
            txtTeacherNameValue.Text = detail.TeacherName;
            txtEmployeeNoValue.Text = detail.EmployeeNo;
            txtStatusValue.Text = detail.Status;
            txtLoadCountValue.Text = detail.LoadCount.ToString();

            lstAdvisory.ItemsSource = detail.Advisories.Count == 0 ? new[] { "(No advisory)" } : detail.Advisories;
            lstHandledSubjects.ItemsSource = detail.HandledSubjects.Count == 0 ? new[] { "(No handled subjects)" } : detail.HandledSubjects;
        }

        private void ClearDetails()
        {
            txtSelectHint.Text = "Select a teacher from the list.";
            txtTeacherNameValue.Text = "-";
            txtEmployeeNoValue.Text = "-";
            txtStatusValue.Text = "-";
            txtLoadCountValue.Text = "-";
            lstAdvisory.ItemsSource = new[] { "(No advisory)" };
            lstHandledSubjects.ItemsSource = new[] { "(No handled subjects)" };
        }
    }
}

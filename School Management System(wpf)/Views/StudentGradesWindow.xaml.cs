using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class StudentGradesWindow : Window
    {
        private sealed class LookupItem
        {
            public LookupItem(long id, string name)
            {
                Id = id;
                Name = name;
            }

            public long Id { get; }
            public string Name { get; }
        }

        private sealed class OfferingLookupItem
        {
            public long Id { get; init; }
            public long SchoolYearId { get; init; }
            public long GradeLevelId { get; init; }
            public long SectionId { get; init; }
            public string Label { get; init; } = string.Empty;
        }

        private sealed class GradeRow : INotifyPropertyChanged
        {
            private string _writtenWorksText = string.Empty;
            private string _performanceTasksText = string.Empty;
            private string _quarterlyAssessmentText = string.Empty;
            private string _quarterGradeText = string.Empty;

            public long? GradeId { get; set; }
            public long StudentId { get; set; }
            public string StudentNumber { get; set; } = string.Empty;
            public string Lrn { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public bool IsLocked { get; set; }
            public DateTime? SubmittedAt { get; set; }
            public DateTime? LockedAt { get; set; }
            public decimal WeightWrittenWorks { get; set; }
            public decimal WeightPerformanceTasks { get; set; }
            public decimal WeightQuarterlyAssessment { get; set; }

            public string WrittenWorksText
            {
                get => _writtenWorksText;
                set
                {
                    if (SetField(ref _writtenWorksText, NormalizeNumericText(value)))
                    {
                        RecalculateQuarterGrade();
                    }
                }
            }

            public string PerformanceTasksText
            {
                get => _performanceTasksText;
                set
                {
                    if (SetField(ref _performanceTasksText, NormalizeNumericText(value)))
                    {
                        RecalculateQuarterGrade();
                    }
                }
            }

            public string QuarterlyAssessmentText
            {
                get => _quarterlyAssessmentText;
                set
                {
                    if (SetField(ref _quarterlyAssessmentText, NormalizeNumericText(value)))
                    {
                        RecalculateQuarterGrade();
                    }
                }
            }

            public string QuarterGradeText
            {
                get => _quarterGradeText;
                private set => SetField(ref _quarterGradeText, value);
            }

            public string SubmittedAtText => SubmittedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";
            public string LockedState => IsLocked ? "Locked" : "Open";

            public event PropertyChangedEventHandler? PropertyChanged;

            public void InitializeGradeValues(decimal? writtenWorks, decimal? performanceTasks, decimal? quarterlyAssessment, decimal? quarterGrade)
            {
                _writtenWorksText = FormatDecimal(writtenWorks);
                _performanceTasksText = FormatDecimal(performanceTasks);
                _quarterlyAssessmentText = FormatDecimal(quarterlyAssessment);
                _quarterGradeText = FormatDecimal(quarterGrade);
            }

            public void RecalculateQuarterGrade()
            {
                var values = new[]
                {
                    ParseDecimal(WrittenWorksText),
                    ParseDecimal(PerformanceTasksText),
                    ParseDecimal(QuarterlyAssessmentText)
                };

                if (values.All(x => !x.HasValue))
                {
                    QuarterGradeText = string.Empty;
                    return;
                }

                var total =
                    (values[0] ?? 0m) * WeightWrittenWorks +
                    (values[1] ?? 0m) * WeightPerformanceTasks +
                    (values[2] ?? 0m) * WeightQuarterlyAssessment;

                QuarterGradeText = total.ToString("0.##", CultureInfo.InvariantCulture);
            }

            private static string NormalizeNumericText(string? value)
            {
                return (value ?? string.Empty).Trim();
            }

            private static string FormatDecimal(decimal? value)
            {
                return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
            }

            private static decimal? ParseDecimal(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return null;
                }

                return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : null;
            }

            private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                {
                    return false;
                }

                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                if (propertyName == nameof(SubmittedAt))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SubmittedAtText)));
                }
                else if (propertyName == nameof(IsLocked))
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LockedState)));
                }

                return true;
            }
        }

        private readonly StudentGradeService _studentGradeService = new();
        private readonly ClassStudentService _classStudentService = new();
        private readonly SchoolYearService _schoolYearService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly SectionService _sectionService = new();
        private readonly ClassOfferingService _classOfferingService = new();
        private readonly StudentService _studentService = new();
        private readonly SubjectService _subjectService = new();
        private readonly TeacherService _teacherService = new();
        private readonly GradingPeriodService _gradingPeriodService = new();
        private readonly GradeComponentService _gradeComponentService = new();

        private List<SchoolYear> _schoolYears = new();
        private List<GradeLevel> _gradeLevels = new();
        private List<Section> _sections = new();
        private List<ClassOffering> _offerings = new();
        private List<Student> _students = new();
        private List<Subject> _subjects = new();
        private List<Teacher> _teachers = new();
        private List<GradingPeriod> _gradingPeriods = new();
        private List<ClassStudent> _classStudents = new();
        private List<StudentGrade> _studentGrades = new();
        private List<GradeComponent> _gradeComponents = new();
        private List<GradeRow> _allRows = new();
        private bool _suppressEvents;

        public StudentGradesWindow()
        {
            InitializeComponent();

            cboSchoolYear.SelectionChanged += FiltersChanged;
            cboGradeLevel.SelectionChanged += FiltersChanged;
            cboSection.SelectionChanged += FiltersChanged;
            cboOffering.SelectionChanged += FiltersChanged;
            cboGradingPeriod.SelectionChanged += FiltersChanged;
            txtSearch.TextChanged += (_, _) => ApplyRosterFilter();
            gridGrades.SelectionChanged += GridGrades_SelectionChanged;
            gridGrades.BeginningEdit += GridGrades_BeginningEdit;
            btnRefresh.Click += (_, _) =>
            {
                LoadLookups();
                LoadRoster();
            };
            btnSave.Click += (_, _) => SaveGrades();

            LoadLookups();
            LoadRoster();
        }

        private void FiltersChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_suppressEvents)
            {
                return;
            }

            if (ReferenceEquals(sender, cboSchoolYear) || ReferenceEquals(sender, cboGradeLevel))
            {
                LoadSectionsCombo();
                LoadOfferingCombo();
                LoadGradingPeriodCombo();
            }
            else if (ReferenceEquals(sender, cboSection))
            {
                LoadOfferingCombo();
            }
            else if (ReferenceEquals(sender, cboOffering))
            {
                SyncFiltersFromOffering();
            }

            LoadRoster();
        }

        private void LoadLookups()
        {
            _schoolYears = _schoolYearService.GetAll()
                .Where(x => !x.IsArchived)
                .OrderByDescending(x => x.StartDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.Id)
                .ToList();
            _gradeLevels = _gradeLevelService.GetAll().OrderBy(x => x.Code).ThenBy(x => x.Name).ToList();
            _sections = _sectionService.GetAll().Where(x => !x.IsArchived).ToList();
            _offerings = _classOfferingService.GetAll().Where(x => x.Status != ClassOfferingStatus.ARCHIVED).ToList();
            _students = _studentService.GetAll().ToList();
            _subjects = _subjectService.GetAll().ToList();
            _teachers = _teacherService.GetAll().ToList();
            _gradingPeriods = _gradingPeriodService.GetAll().ToList();
            _classStudents = _classStudentService.GetAll().Where(x => x.Status == ClassStudentStatus.ACTIVE).ToList();
            _studentGrades = _studentGradeService.GetAll().ToList();
            _gradeComponents = _gradeComponentService.GetAll().Where(x => x.IsActive).ToList();

            var preferredSchoolYearId = _schoolYears
                .FirstOrDefault(x => x.Status == SchoolYearStatus.ACTIVE)?.Id
                ?? _schoolYears.FirstOrDefault()?.Id
                ?? 0L;

            _suppressEvents = true;

            cboSchoolYear.ItemsSource = _schoolYears.Select(x => new LookupItem(x.Id, x.Name)).ToList();
            cboSchoolYear.DisplayMemberPath = nameof(LookupItem.Name);
            cboSchoolYear.SelectedValuePath = nameof(LookupItem.Id);
            cboSchoolYear.SelectedValue = preferredSchoolYearId;

            cboGradeLevel.ItemsSource = _gradeLevels.Select(x => new LookupItem(x.Id, string.IsNullOrWhiteSpace(x.Code) ? x.Name : x.Code)).ToList();
            cboGradeLevel.DisplayMemberPath = nameof(LookupItem.Name);
            cboGradeLevel.SelectedValuePath = nameof(LookupItem.Id);
            cboGradeLevel.SelectedIndex = _gradeLevels.Count > 0 ? 0 : -1;

            LoadSectionsCombo();
            LoadOfferingCombo();
            LoadGradingPeriodCombo();

            _suppressEvents = false;
            UpdateWeightsHint();
        }

        private void LoadSectionsCombo()
        {
            var schoolYearId = cboSchoolYear.SelectedValue is long sy && sy > 0 ? sy : (long?)null;
            var gradeLevelId = cboGradeLevel.SelectedValue is long gl && gl > 0 ? gl : (long?)null;
            var selectedSectionId = cboSection.SelectedValue is long sectionId ? sectionId : 0L;

            var items = _sections
                .Where(x => (!schoolYearId.HasValue || x.SchoolYearId == schoolYearId.Value) &&
                            (!gradeLevelId.HasValue || x.GradeLevelId == gradeLevelId.Value))
                .OrderBy(x => x.Name)
                .Select(x => new LookupItem(x.Id, x.Name))
                .ToList();

            cboSection.ItemsSource = items;
            cboSection.DisplayMemberPath = nameof(LookupItem.Name);
            cboSection.SelectedValuePath = nameof(LookupItem.Id);
            cboSection.SelectedValue = items.Any(x => x.Id == selectedSectionId)
                ? selectedSectionId
                : items.FirstOrDefault()?.Id ?? 0L;
        }

        private void LoadOfferingCombo()
        {
            var schoolYearId = cboSchoolYear.SelectedValue is long sy && sy > 0 ? sy : (long?)null;
            var gradeLevelId = cboGradeLevel.SelectedValue is long gl && gl > 0 ? gl : (long?)null;
            var sectionId = cboSection.SelectedValue is long section && section > 0 ? section : (long?)null;
            var selectedOfferingId = cboOffering.SelectedValue is long offeringId ? offeringId : 0L;

            var items = _offerings
                .Where(x =>
                {
                    var section = _sections.FirstOrDefault(s => s.Id == x.SectionId);
                    if (section == null)
                    {
                        return false;
                    }

                    return (!schoolYearId.HasValue || x.SchoolYearId == schoolYearId.Value) &&
                           (!gradeLevelId.HasValue || section.GradeLevelId == gradeLevelId.Value) &&
                           (!sectionId.HasValue || x.SectionId == sectionId.Value);
                })
                .OrderBy(x => x.SectionId)
                .ThenBy(x => x.SubjectId)
                .Select(BuildOfferingLookupItem)
                .ToList();

            cboOffering.ItemsSource = items;
            cboOffering.DisplayMemberPath = nameof(OfferingLookupItem.Label);
            cboOffering.SelectedValuePath = nameof(OfferingLookupItem.Id);
            cboOffering.SelectedValue = items.Any(x => x.Id == selectedOfferingId)
                ? selectedOfferingId
                : items.FirstOrDefault()?.Id ?? 0L;
        }

        private void LoadGradingPeriodCombo()
        {
            var schoolYearId = cboSchoolYear.SelectedValue is long sy && sy > 0 ? sy : (long?)null;
            var selectedId = cboGradingPeriod.SelectedValue is long id ? id : 0L;

            var items = _gradingPeriods
                .Where(x => !schoolYearId.HasValue || x.SchoolYearId == schoolYearId.Value)
                .OrderBy(x => x.StartDate ?? DateTime.MinValue)
                .ThenBy(x => x.Name)
                .Select(x => new LookupItem(x.Id, $"{x.Name} ({x.Status})"))
                .ToList();

            cboGradingPeriod.ItemsSource = items;
            cboGradingPeriod.DisplayMemberPath = nameof(LookupItem.Name);
            cboGradingPeriod.SelectedValuePath = nameof(LookupItem.Id);
            cboGradingPeriod.SelectedValue = items.Any(x => x.Id == selectedId)
                ? selectedId
                : items.FirstOrDefault()?.Id ?? 0L;
        }

        private void SyncFiltersFromOffering()
        {
            if (cboOffering.SelectedItem is not OfferingLookupItem offering)
            {
                return;
            }

            _suppressEvents = true;
            cboSchoolYear.SelectedValue = offering.SchoolYearId;
            cboGradeLevel.SelectedValue = offering.GradeLevelId;
            LoadSectionsCombo();
            cboSection.SelectedValue = offering.SectionId;
            LoadGradingPeriodCombo();
            _suppressEvents = false;
        }

        private void LoadRoster()
        {
            UpdateWeightsHint();

            if (cboOffering.SelectedValue is not long offeringId || offeringId <= 0 ||
                cboGradingPeriod.SelectedValue is not long gradingPeriodId || gradingPeriodId <= 0)
            {
                _allRows = new List<GradeRow>();
                gridGrades.ItemsSource = _allRows;
                txtSummary.Text = "Select an offering and grading period to load grades.";
                txtRosterHint.Text = "Select a school year, class offering, and grading period to load the roster.";
                txtOfferingValue.Text = "-";
                txtPeriodValue.Text = "-";
                ClearSelectedRowDetails();
                return;
            }

            var selectedOffering = _offerings.FirstOrDefault(x => x.Id == offeringId);
            var selectedPeriod = _gradingPeriods.FirstOrDefault(x => x.Id == gradingPeriodId);
            var roster = _classStudents
                .Where(x => x.ClassOfferingId == offeringId)
                .OrderBy(x => ResolveStudentName(x.StudentId))
                .ToList();

            var wwWeight = ResolveWeight(GradeComponentName.WRITTEN_WORKS);
            var ptWeight = ResolveWeight(GradeComponentName.PERFORMANCE_TASKS);
            var qaWeight = ResolveWeight(GradeComponentName.QUARTERLY_ASSESSMENT);

            _allRows = roster.Select(classStudent =>
            {
                var student = _students.FirstOrDefault(x => x.Id == classStudent.StudentId);
                var existingGrade = _studentGrades.FirstOrDefault(x =>
                    x.ClassOfferingId == offeringId &&
                    x.GradingPeriodId == gradingPeriodId &&
                    x.StudentId == classStudent.StudentId);

                var row = new GradeRow
                {
                    GradeId = existingGrade?.Id,
                    StudentId = classStudent.StudentId,
                    StudentNumber = student?.StudentNumber ?? string.Empty,
                    Lrn = student?.Lrn ?? string.Empty,
                    StudentName = ResolveStudentName(classStudent.StudentId),
                    IsLocked = existingGrade?.LockedAt.HasValue == true || selectedPeriod?.Status is GradingPeriodStatus.LOCKED or GradingPeriodStatus.POSTED,
                    SubmittedAt = existingGrade?.SubmittedAt,
                    LockedAt = existingGrade?.LockedAt,
                    WeightWrittenWorks = wwWeight,
                    WeightPerformanceTasks = ptWeight,
                    WeightQuarterlyAssessment = qaWeight
                };
                row.InitializeGradeValues(existingGrade?.WrittenWorks, existingGrade?.PerformanceTasks, existingGrade?.QuarterlyAssessment, existingGrade?.QuarterGrade);
                row.RecalculateQuarterGrade();
                return row;
            }).ToList();

            txtOfferingValue.Text = BuildOfferingLabel(selectedOffering);
            txtPeriodValue.Text = selectedPeriod == null ? "-" : $"{selectedPeriod.Name} ({selectedPeriod.Status})";
            txtRosterHint.Text = _allRows.Count == 0
                ? "No active class roster was found for the selected offering."
                : "Edit unlocked roster rows, then save to persist student grades.";

            ApplyRosterFilter();
        }

        private void ApplyRosterFilter()
        {
            var search = (txtSearch.Text ?? string.Empty).Trim();
            var filtered = string.IsNullOrWhiteSpace(search)
                ? _allRows
                : _allRows.Where(x =>
                    x.StudentNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Lrn.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.StudentName.Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            gridGrades.ItemsSource = filtered;
            txtSummary.Text = $"Roster Rows: {filtered.Count} of {_allRows.Count}";
            gridGrades.SelectedItem = null;
            ClearSelectedRowDetails();
        }

        private void SaveGrades()
        {
            if (cboOffering.SelectedValue is not long offeringId || offeringId <= 0 ||
                cboGradingPeriod.SelectedValue is not long gradingPeriodId || gradingPeriodId <= 0)
            {
                MessageBox.Show("Select an offering and grading period first.", "Student Grades", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editableRows = _allRows.Where(x => !x.IsLocked).ToList();
            if (editableRows.Count == 0)
            {
                MessageBox.Show("No editable grade rows are available for the selected roster.", "Student Grades", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var row in editableRows)
            {
                if (!TryParseGradeRow(row, out var writtenWorks, out var performanceTasks, out var quarterlyAssessment, out var quarterGrade, out var errorMessage))
                {
                    MessageBox.Show($"{row.StudentName}: {errorMessage}", "Student Grades", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existing = row.GradeId.HasValue
                    ? _studentGrades.FirstOrDefault(x => x.Id == row.GradeId.Value)
                    : _studentGrades.FirstOrDefault(x => x.ClassOfferingId == offeringId && x.GradingPeriodId == gradingPeriodId && x.StudentId == row.StudentId);

                var hasAnyScore = writtenWorks.HasValue || performanceTasks.HasValue || quarterlyAssessment.HasValue;
                if (!hasAnyScore && existing == null)
                {
                    continue;
                }

                var now = DateTime.UtcNow;
                if (existing == null)
                {
                    var entity = new StudentGrade
                    {
                        ClassOfferingId = offeringId,
                        GradingPeriodId = gradingPeriodId,
                        StudentId = row.StudentId,
                        WrittenWorks = writtenWorks,
                        PerformanceTasks = performanceTasks,
                        QuarterlyAssessment = quarterlyAssessment,
                        QuarterGrade = quarterGrade,
                        SubmittedAt = hasAnyScore ? now : null,
                        LockedAt = null,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _studentGradeService.Create(entity);
                    AuditTrailService.Log("CREATE", "student_grades", entity.Id, null, entity);
                }
                else
                {
                    var oldData = new
                    {
                        existing.WrittenWorks,
                        existing.PerformanceTasks,
                        existing.QuarterlyAssessment,
                        existing.QuarterGrade,
                        existing.SubmittedAt
                    };

                    existing.WrittenWorks = writtenWorks;
                    existing.PerformanceTasks = performanceTasks;
                    existing.QuarterlyAssessment = quarterlyAssessment;
                    existing.QuarterGrade = quarterGrade;
                    existing.SubmittedAt = hasAnyScore ? (existing.SubmittedAt ?? now) : null;
                    existing.UpdatedAt = now;

                    _studentGradeService.Update(existing);
                    AuditTrailService.Log("UPDATE", "student_grades", existing.Id, oldData, existing);
                }
            }

            _studentGrades = _studentGradeService.GetAll().ToList();
            LoadRoster();
            MessageBox.Show("Student grades saved.", "Student Grades", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GridGrades_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridGrades.SelectedItem is not GradeRow row)
            {
                ClearSelectedRowDetails();
                return;
            }

            txtStudentValue.Text = row.StudentName;
            txtLockStateValue.Text = row.LockedState;
            txtWrittenWorksValue.Text = string.IsNullOrWhiteSpace(row.WrittenWorksText) ? "-" : row.WrittenWorksText;
            txtPerformanceTasksValue.Text = string.IsNullOrWhiteSpace(row.PerformanceTasksText) ? "-" : row.PerformanceTasksText;
            txtQuarterlyAssessmentValue.Text = string.IsNullOrWhiteSpace(row.QuarterlyAssessmentText) ? "-" : row.QuarterlyAssessmentText;
            txtQuarterGradeValue.Text = string.IsNullOrWhiteSpace(row.QuarterGradeText) ? "-" : row.QuarterGradeText;
            txtTimestampsValue.Text = $"Submitted: {row.SubmittedAtText} | Locked: {(row.LockedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-")}";
        }

        private void GridGrades_BeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is GradeRow row && row.IsLocked)
            {
                e.Cancel = true;
                MessageBox.Show("This grade row is locked and cannot be edited.", "Student Grades", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateWeightsHint()
        {
            var wwWeight = ResolveWeight(GradeComponentName.WRITTEN_WORKS);
            var ptWeight = ResolveWeight(GradeComponentName.PERFORMANCE_TASKS);
            var qaWeight = ResolveWeight(GradeComponentName.QUARTERLY_ASSESSMENT);
            txtWeightsHint.Text = $"Quarter grade weights: Written Works {wwWeight:P0}, Performance Tasks {ptWeight:P0}, Quarterly Assessment {qaWeight:P0}.";
        }

        private void ClearSelectedRowDetails()
        {
            txtStudentValue.Text = "-";
            txtLockStateValue.Text = "-";
            txtWrittenWorksValue.Text = "-";
            txtPerformanceTasksValue.Text = "-";
            txtQuarterlyAssessmentValue.Text = "-";
            txtQuarterGradeValue.Text = "-";
            txtTimestampsValue.Text = "-";
        }

        private OfferingLookupItem BuildOfferingLookupItem(ClassOffering offering)
        {
            var section = _sections.FirstOrDefault(x => x.Id == offering.SectionId);
            var subject = _subjects.FirstOrDefault(x => x.Id == offering.SubjectId);
            var teacher = offering.TeacherId.HasValue
                ? _teachers.FirstOrDefault(x => x.Id == offering.TeacherId.Value)
                : null;
            var gradeLabel = section == null
                ? "N/A"
                : _gradeLevels.FirstOrDefault(x => x.Id == section.GradeLevelId)?.Code
                  ?? _gradeLevels.FirstOrDefault(x => x.Id == section.GradeLevelId)?.Name
                  ?? "N/A";
            var teacherLabel = teacher == null ? "Unassigned" : $"{teacher.LastName}, {teacher.FirstName}";

            return new OfferingLookupItem
            {
                Id = offering.Id,
                SchoolYearId = offering.SchoolYearId,
                GradeLevelId = section?.GradeLevelId ?? 0L,
                SectionId = offering.SectionId,
                Label = $"{gradeLabel}-{section?.Name ?? "N/A"} | {subject?.Title ?? "Unknown Subject"} | {teacherLabel}"
            };
        }

        private string BuildOfferingLabel(ClassOffering? offering)
        {
            if (offering == null)
            {
                return "-";
            }

            return (cboOffering.ItemsSource as IEnumerable<OfferingLookupItem>)?.FirstOrDefault(x => x.Id == offering.Id)?.Label
                   ?? $"Offering #{offering.Id}";
        }

        private string ResolveStudentName(long studentId)
        {
            var student = _students.FirstOrDefault(x => x.Id == studentId);
            return student == null ? $"Student {studentId}" : $"{student.LastName}, {student.FirstName}";
        }

        private decimal ResolveWeight(GradeComponentName componentName)
        {
            return _gradeComponents.FirstOrDefault(x => x.Name == componentName)?.Weight ?? 0m;
        }

        private static bool TryParseGradeRow(GradeRow row, out decimal? writtenWorks, out decimal? performanceTasks, out decimal? quarterlyAssessment, out decimal? quarterGrade, out string errorMessage)
        {
            writtenWorks = null;
            performanceTasks = null;
            quarterlyAssessment = null;
            quarterGrade = null;
            errorMessage = string.Empty;

            if (!TryParseScore(row.WrittenWorksText, nameof(row.WrittenWorksText), out writtenWorks, out errorMessage) ||
                !TryParseScore(row.PerformanceTasksText, nameof(row.PerformanceTasksText), out performanceTasks, out errorMessage) ||
                !TryParseScore(row.QuarterlyAssessmentText, nameof(row.QuarterlyAssessmentText), out quarterlyAssessment, out errorMessage))
            {
                return false;
            }

            quarterGrade = string.IsNullOrWhiteSpace(row.QuarterGradeText)
                ? null
                : decimal.Parse(row.QuarterGradeText, NumberStyles.Number, CultureInfo.InvariantCulture);
            return true;
        }

        private static bool TryParseScore(string rawValue, string fieldName, out decimal? parsedValue, out string errorMessage)
        {
            parsedValue = null;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return true;
            }

            if (!decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var score))
            {
                errorMessage = $"{fieldName} must be a valid number.";
                return false;
            }

            if (score < 0m || score > 100m)
            {
                errorMessage = $"{fieldName} must be between 0 and 100.";
                return false;
            }

            parsedValue = Math.Round(score, 2);
            return true;
        }
    }
}

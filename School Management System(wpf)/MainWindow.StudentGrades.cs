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

namespace School_Management_System
{
    public partial class MainWindow
    {
        private const int StudentGradesTabIndex = 10;

        private sealed class SGLookupItem
        {
            public SGLookupItem(long id, string name) { Id = id; Name = name; }
            public long Id { get; }
            public string Name { get; }
        }

        private sealed class SGOfferingLookupItem
        {
            public long Id { get; init; }
            public long SchoolYearId { get; init; }
            public long GradeLevelId { get; init; }
            public long SectionId { get; init; }
            public string Label { get; init; } = string.Empty;
        }

        private sealed class SGGradeRow : INotifyPropertyChanged
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
                set { if (SGSetField(ref _writtenWorksText, SGNormalizeNumericText(value))) RecalculateQuarterGrade(); }
            }

            public string PerformanceTasksText
            {
                get => _performanceTasksText;
                set { if (SGSetField(ref _performanceTasksText, SGNormalizeNumericText(value))) RecalculateQuarterGrade(); }
            }

            public string QuarterlyAssessmentText
            {
                get => _quarterlyAssessmentText;
                set { if (SGSetField(ref _quarterlyAssessmentText, SGNormalizeNumericText(value))) RecalculateQuarterGrade(); }
            }

            public string QuarterGradeText
            {
                get => _quarterGradeText;
                private set => SGSetField(ref _quarterGradeText, value);
            }

            public string SubmittedAtText => SubmittedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-";
            public string LockedState => IsLocked ? "Locked" : "Open";

            public event PropertyChangedEventHandler? PropertyChanged;

            public void InitializeGradeValues(decimal? ww, decimal? pt, decimal? qa, decimal? qg)
            {
                _writtenWorksText = SGFormatDecimal(ww);
                _performanceTasksText = SGFormatDecimal(pt);
                _quarterlyAssessmentText = SGFormatDecimal(qa);
                _quarterGradeText = SGFormatDecimal(qg);
            }

            public void RecalculateQuarterGrade()
            {
                var values = new[] { SGParseDecimal(WrittenWorksText), SGParseDecimal(PerformanceTasksText), SGParseDecimal(QuarterlyAssessmentText) };
                if (values.All(x => !x.HasValue)) { QuarterGradeText = string.Empty; return; }
                var total = (values[0] ?? 0m) * WeightWrittenWorks + (values[1] ?? 0m) * WeightPerformanceTasks + (values[2] ?? 0m) * WeightQuarterlyAssessment;
                QuarterGradeText = total.ToString("0.##", CultureInfo.InvariantCulture);
            }

            private static string SGNormalizeNumericText(string? value) => (value ?? string.Empty).Trim();
            private static string SGFormatDecimal(decimal? value) => value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
            private static decimal? SGParseDecimal(string? value) => string.IsNullOrWhiteSpace(value) ? null : decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var p) ? p : null;

            private bool SGSetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
            {
                if (EqualityComparer<T>.Default.Equals(field, value)) return false;
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
        }

        private readonly StudentGradeService _sgGradeService = new();
        private readonly ClassStudentService _sgClassStudentService = new();
        private readonly GradingPeriodService _sgGradingPeriodService = new();
        private readonly GradeComponentService _sgGradeComponentService = new();

        private List<SchoolYear> _sgSchoolYears = new();
        private List<GradeLevel> _sgGradeLevels = new();
        private List<Section> _sgSections = new();
        private List<ClassOffering> _sgOfferings = new();
        private List<Student> _sgStudents = new();
        private List<Subject> _sgSubjects = new();
        private List<Teacher> _sgTeachers = new();
        private List<GradingPeriod> _sgGradingPeriods = new();
        private List<ClassStudent> _sgClassStudents = new();
        private List<StudentGrade> _sgStudentGrades = new();
        private List<GradeComponent> _sgGradeComponents = new();
        private List<SGGradeRow> _sgAllRows = new();
        private bool _sgSuppressEvents;

        private void InitializeStudentGradesTab()
        {
            cboSGSchoolYear.SelectionChanged += SGFiltersChanged;
            cboSGGradeLevel.SelectionChanged += SGFiltersChanged;
            cboSGSection.SelectionChanged += SGFiltersChanged;
            cboSGOffering.SelectionChanged += SGFiltersChanged;
            cboSGGradingPeriod.SelectionChanged += SGFiltersChanged;
            txtSGSearch.TextChanged += (_, _) => SGApplyRosterFilter();
            gridSGGrades.SelectionChanged += SGGridGrades_SelectionChanged;
            gridSGGrades.BeginningEdit += SGGridGrades_BeginningEdit;
            btnSGRefresh.Click += (_, _) => { SGLoadLookups(); SGLoadRoster(); };
            btnSGSave.Click += (_, _) => SGSaveGrades();
        }

        private void NavigateToStudentGrades()
        {
            SGLoadLookups();
            SGLoadRoster();
            NavigateMainTab(StudentGradesTabIndex);
        }

        private void SGFiltersChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_sgSuppressEvents) return;
            if (ReferenceEquals(sender, cboSGSchoolYear) || ReferenceEquals(sender, cboSGGradeLevel))
            {
                SGLoadSectionsCombo();
                SGLoadOfferingCombo();
                SGLoadGradingPeriodCombo();
            }
            else if (ReferenceEquals(sender, cboSGSection))
            {
                SGLoadOfferingCombo();
            }
            else if (ReferenceEquals(sender, cboSGOffering))
            {
                SGSyncFiltersFromOffering();
            }
            SGLoadRoster();
        }

        private void SGLoadLookups()
        {
            _sgSchoolYears = _schoolYearService.GetAll().Where(x => !x.IsArchived).OrderByDescending(x => x.StartDate ?? DateTime.MinValue).ThenByDescending(x => x.Id).ToList();
            _sgGradeLevels = _gradeLevelService.GetAll().OrderBy(x => x.Code).ThenBy(x => x.Name).ToList();
            _sgSections = _sectionService.GetAll().Where(x => !x.IsArchived).ToList();
            _sgOfferings = _classOfferingService.GetAll().Where(x => x.Status != ClassOfferingStatus.ARCHIVED).ToList();
            _sgStudents = _studentService.GetAll().ToList();
            _sgSubjects = _subjectService.GetAll().ToList();
            _sgTeachers = _teacherService.GetAll().ToList();
            _sgGradingPeriods = _sgGradingPeriodService.GetAll().ToList();
            _sgClassStudents = _sgClassStudentService.GetAll().Where(x => x.Status == ClassStudentStatus.ACTIVE).ToList();
            _sgStudentGrades = _sgGradeService.GetAll().ToList();
            _sgGradeComponents = _sgGradeComponentService.GetAll().Where(x => x.IsActive).ToList();

            var preferredSchoolYearId = _sgSchoolYears.FirstOrDefault(x => x.Status == SchoolYearStatus.ACTIVE)?.Id ?? _sgSchoolYears.FirstOrDefault()?.Id ?? 0L;

            _sgSuppressEvents = true;

            cboSGSchoolYear.ItemsSource = _sgSchoolYears.Select(x => new SGLookupItem(x.Id, x.Name)).ToList();
            cboSGSchoolYear.DisplayMemberPath = nameof(SGLookupItem.Name);
            cboSGSchoolYear.SelectedValuePath = nameof(SGLookupItem.Id);
            cboSGSchoolYear.SelectedValue = preferredSchoolYearId;

            cboSGGradeLevel.ItemsSource = _sgGradeLevels.Select(x => new SGLookupItem(x.Id, string.IsNullOrWhiteSpace(x.Code) ? x.Name : x.Code)).ToList();
            cboSGGradeLevel.DisplayMemberPath = nameof(SGLookupItem.Name);
            cboSGGradeLevel.SelectedValuePath = nameof(SGLookupItem.Id);
            cboSGGradeLevel.SelectedIndex = _sgGradeLevels.Count > 0 ? 0 : -1;

            SGLoadSectionsCombo();
            SGLoadOfferingCombo();
            SGLoadGradingPeriodCombo();

            _sgSuppressEvents = false;
            SGUpdateWeightsHint();
        }

        private void SGLoadSectionsCombo()
        {
            var schoolYearId = cboSGSchoolYear.SelectedValue is long sy && sy > 0 ? sy : (long?)null;
            var gradeLevelId = cboSGGradeLevel.SelectedValue is long gl && gl > 0 ? gl : (long?)null;
            var selectedSectionId = cboSGSection.SelectedValue is long sectionId ? sectionId : 0L;

            var items = _sgSections
                .Where(x => (!schoolYearId.HasValue || x.SchoolYearId == schoolYearId.Value) && (!gradeLevelId.HasValue || x.GradeLevelId == gradeLevelId.Value))
                .OrderBy(x => x.Name)
                .Select(x => new SGLookupItem(x.Id, x.Name))
                .ToList();

            cboSGSection.ItemsSource = items;
            cboSGSection.DisplayMemberPath = nameof(SGLookupItem.Name);
            cboSGSection.SelectedValuePath = nameof(SGLookupItem.Id);
            cboSGSection.SelectedValue = items.Any(x => x.Id == selectedSectionId) ? selectedSectionId : items.FirstOrDefault()?.Id ?? 0L;
        }

        private void SGLoadOfferingCombo()
        {
            var schoolYearId = cboSGSchoolYear.SelectedValue is long sy && sy > 0 ? sy : (long?)null;
            var gradeLevelId = cboSGGradeLevel.SelectedValue is long gl && gl > 0 ? gl : (long?)null;
            var sectionId = cboSGSection.SelectedValue is long section && section > 0 ? section : (long?)null;
            var selectedOfferingId = cboSGOffering.SelectedValue is long offeringId ? offeringId : 0L;

            var items = _sgOfferings
                .Where(x =>
                {
                    var sec = _sgSections.FirstOrDefault(s => s.Id == x.SectionId);
                    if (sec == null) return false;
                    return (!schoolYearId.HasValue || x.SchoolYearId == schoolYearId.Value) &&
                           (!gradeLevelId.HasValue || sec.GradeLevelId == gradeLevelId.Value) &&
                           (!sectionId.HasValue || x.SectionId == sectionId.Value);
                })
                .OrderBy(x => x.SectionId).ThenBy(x => x.SubjectId)
                .Select(SGBuildOfferingLookupItem)
                .ToList();

            cboSGOffering.ItemsSource = items;
            cboSGOffering.DisplayMemberPath = nameof(SGOfferingLookupItem.Label);
            cboSGOffering.SelectedValuePath = nameof(SGOfferingLookupItem.Id);
            cboSGOffering.SelectedValue = items.Any(x => x.Id == selectedOfferingId) ? selectedOfferingId : items.FirstOrDefault()?.Id ?? 0L;
        }

        private void SGLoadGradingPeriodCombo()
        {
            var schoolYearId = cboSGSchoolYear.SelectedValue is long sy && sy > 0 ? sy : (long?)null;
            var selectedId = cboSGGradingPeriod.SelectedValue is long id ? id : 0L;

            var items = _sgGradingPeriods
                .Where(x => !schoolYearId.HasValue || x.SchoolYearId == schoolYearId.Value)
                .OrderBy(x => x.StartDate ?? DateTime.MinValue).ThenBy(x => x.Name)
                .Select(x => new SGLookupItem(x.Id, $"{x.Name} ({x.Status})"))
                .ToList();

            cboSGGradingPeriod.ItemsSource = items;
            cboSGGradingPeriod.DisplayMemberPath = nameof(SGLookupItem.Name);
            cboSGGradingPeriod.SelectedValuePath = nameof(SGLookupItem.Id);
            cboSGGradingPeriod.SelectedValue = items.Any(x => x.Id == selectedId) ? selectedId : items.FirstOrDefault()?.Id ?? 0L;
        }

        private void SGSyncFiltersFromOffering()
        {
            if (cboSGOffering.SelectedItem is not SGOfferingLookupItem offering) return;
            _sgSuppressEvents = true;
            cboSGSchoolYear.SelectedValue = offering.SchoolYearId;
            cboSGGradeLevel.SelectedValue = offering.GradeLevelId;
            SGLoadSectionsCombo();
            cboSGSection.SelectedValue = offering.SectionId;
            SGLoadGradingPeriodCombo();
            _sgSuppressEvents = false;
        }

        private void SGLoadRoster()
        {
            SGUpdateWeightsHint();

            if (cboSGOffering.SelectedValue is not long offeringId || offeringId <= 0 ||
                cboSGGradingPeriod.SelectedValue is not long gradingPeriodId || gradingPeriodId <= 0)
            {
                _sgAllRows = new List<SGGradeRow>();
                gridSGGrades.ItemsSource = _sgAllRows;
                txtSGSummary.Text = "Select an offering and grading period to load grades.";
                txtSGRosterHint.Text = "Select a school year, class offering, and grading period to load the roster.";
                txtSGOfferingValue.Text = "-";
                txtSGPeriodValue.Text = "-";
                SGClearSelectedRowDetails();
                return;
            }

            var selectedOffering = _sgOfferings.FirstOrDefault(x => x.Id == offeringId);
            var selectedPeriod = _sgGradingPeriods.FirstOrDefault(x => x.Id == gradingPeriodId);
            var roster = _sgClassStudents.Where(x => x.ClassOfferingId == offeringId).OrderBy(x => SGResolveStudentName(x.StudentId)).ToList();

            var wwWeight = SGResolveWeight(GradeComponentName.WRITTEN_WORKS);
            var ptWeight = SGResolveWeight(GradeComponentName.PERFORMANCE_TASKS);
            var qaWeight = SGResolveWeight(GradeComponentName.QUARTERLY_ASSESSMENT);

            _sgAllRows = roster.Select(cs =>
            {
                var student = _sgStudents.FirstOrDefault(x => x.Id == cs.StudentId);
                var existingGrade = _sgStudentGrades.FirstOrDefault(x => x.ClassOfferingId == offeringId && x.GradingPeriodId == gradingPeriodId && x.StudentId == cs.StudentId);

                var row = new SGGradeRow
                {
                    GradeId = existingGrade?.Id,
                    StudentId = cs.StudentId,
                    StudentNumber = student?.StudentNumber ?? string.Empty,
                    Lrn = student?.Lrn ?? string.Empty,
                    StudentName = SGResolveStudentName(cs.StudentId),
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

            txtSGOfferingValue.Text = SGBuildOfferingLabel(selectedOffering);
            txtSGPeriodValue.Text = selectedPeriod == null ? "-" : $"{selectedPeriod.Name} ({selectedPeriod.Status})";
            txtSGRosterHint.Text = _sgAllRows.Count == 0
                ? "No active class roster was found for the selected offering."
                : "Edit unlocked roster rows, then save to persist student grades.";

            SGApplyRosterFilter();
        }

        private void SGApplyRosterFilter()
        {
            var search = (txtSGSearch.Text ?? string.Empty).Trim();
            var filtered = string.IsNullOrWhiteSpace(search)
                ? _sgAllRows
                : _sgAllRows.Where(x =>
                    x.StudentNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.Lrn.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    x.StudentName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

            gridSGGrades.ItemsSource = filtered;
            txtSGSummary.Text = $"Roster Rows: {filtered.Count} of {_sgAllRows.Count}";
            gridSGGrades.SelectedItem = null;
            SGClearSelectedRowDetails();
        }

        private void SGSaveGrades()
        {
            if (cboSGOffering.SelectedValue is not long offeringId || offeringId <= 0 ||
                cboSGGradingPeriod.SelectedValue is not long gradingPeriodId || gradingPeriodId <= 0)
            {
                MessageBox.Show("Select an offering and grading period first.", "Student Grades", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editableRows = _sgAllRows.Where(x => !x.IsLocked).ToList();
            if (editableRows.Count == 0)
            {
                MessageBox.Show("No editable grade rows are available for the selected roster.", "Student Grades", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var row in editableRows)
            {
                if (!SGTryParseGradeRow(row, out var writtenWorks, out var performanceTasks, out var quarterlyAssessment, out var quarterGrade, out var errorMessage))
                {
                    MessageBox.Show($"{row.StudentName}: {errorMessage}", "Student Grades", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existing = row.GradeId.HasValue
                    ? _sgStudentGrades.FirstOrDefault(x => x.Id == row.GradeId.Value)
                    : _sgStudentGrades.FirstOrDefault(x => x.ClassOfferingId == offeringId && x.GradingPeriodId == gradingPeriodId && x.StudentId == row.StudentId);

                var hasAnyScore = writtenWorks.HasValue || performanceTasks.HasValue || quarterlyAssessment.HasValue;
                if (!hasAnyScore && existing == null) continue;

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
                    _sgGradeService.Create(entity);
                    AuditTrailService.Log("CREATE", "student_grades", entity.Id, null, entity);
                }
                else
                {
                    var oldData = new { existing.WrittenWorks, existing.PerformanceTasks, existing.QuarterlyAssessment, existing.QuarterGrade, existing.SubmittedAt };
                    existing.WrittenWorks = writtenWorks;
                    existing.PerformanceTasks = performanceTasks;
                    existing.QuarterlyAssessment = quarterlyAssessment;
                    existing.QuarterGrade = quarterGrade;
                    existing.SubmittedAt = hasAnyScore ? (existing.SubmittedAt ?? now) : null;
                    existing.UpdatedAt = now;
                    _sgGradeService.Update(existing);
                    AuditTrailService.Log("UPDATE", "student_grades", existing.Id, oldData, existing);
                }
            }

            _sgStudentGrades = _sgGradeService.GetAll().ToList();
            SGLoadRoster();
            MessageBox.Show("Student grades saved.", "Student Grades", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SGGridGrades_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridSGGrades.SelectedItem is not SGGradeRow row) { SGClearSelectedRowDetails(); return; }
            txtSGStudentValue.Text = row.StudentName;
            txtSGLockStateValue.Text = row.LockedState;
            txtSGWrittenWorksValue.Text = string.IsNullOrWhiteSpace(row.WrittenWorksText) ? "-" : row.WrittenWorksText;
            txtSGPerformanceTasksValue.Text = string.IsNullOrWhiteSpace(row.PerformanceTasksText) ? "-" : row.PerformanceTasksText;
            txtSGQuarterlyAssessmentValue.Text = string.IsNullOrWhiteSpace(row.QuarterlyAssessmentText) ? "-" : row.QuarterlyAssessmentText;
            txtSGQuarterGradeValue.Text = string.IsNullOrWhiteSpace(row.QuarterGradeText) ? "-" : row.QuarterGradeText;
            txtSGTimestampsValue.Text = $"Submitted: {row.SubmittedAtText} | Locked: {(row.LockedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-")}";
        }

        private void SGGridGrades_BeginningEdit(object? sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Row.Item is SGGradeRow row && row.IsLocked)
            {
                e.Cancel = true;
                MessageBox.Show("This grade row is locked and cannot be edited.", "Student Grades", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SGUpdateWeightsHint()
        {
            var ww = SGResolveWeight(GradeComponentName.WRITTEN_WORKS);
            var pt = SGResolveWeight(GradeComponentName.PERFORMANCE_TASKS);
            var qa = SGResolveWeight(GradeComponentName.QUARTERLY_ASSESSMENT);
            txtSGWeightsHint.Text = $"Quarter grade weights: Written Works {ww:P0}, Performance Tasks {pt:P0}, Quarterly Assessment {qa:P0}.";
        }

        private void SGClearSelectedRowDetails()
        {
            txtSGStudentValue.Text = "-";
            txtSGLockStateValue.Text = "-";
            txtSGWrittenWorksValue.Text = "-";
            txtSGPerformanceTasksValue.Text = "-";
            txtSGQuarterlyAssessmentValue.Text = "-";
            txtSGQuarterGradeValue.Text = "-";
            txtSGTimestampsValue.Text = "-";
        }

        private SGOfferingLookupItem SGBuildOfferingLookupItem(ClassOffering offering)
        {
            var section = _sgSections.FirstOrDefault(x => x.Id == offering.SectionId);
            var subject = _sgSubjects.FirstOrDefault(x => x.Id == offering.SubjectId);
            var teacher = offering.TeacherId.HasValue ? _sgTeachers.FirstOrDefault(x => x.Id == offering.TeacherId.Value) : null;
            var gradeLabel = section == null ? "N/A" : _sgGradeLevels.FirstOrDefault(x => x.Id == section.GradeLevelId)?.Code ?? _sgGradeLevels.FirstOrDefault(x => x.Id == section.GradeLevelId)?.Name ?? "N/A";
            var teacherLabel = teacher == null ? "Unassigned" : $"{teacher.LastName}, {teacher.FirstName}";

            return new SGOfferingLookupItem
            {
                Id = offering.Id,
                SchoolYearId = offering.SchoolYearId,
                GradeLevelId = section?.GradeLevelId ?? 0L,
                SectionId = offering.SectionId,
                Label = $"{gradeLabel}-{section?.Name ?? "N/A"} | {subject?.Title ?? "Unknown Subject"} | {teacherLabel}"
            };
        }

        private string SGBuildOfferingLabel(ClassOffering? offering)
        {
            if (offering == null) return "-";
            return (cboSGOffering.ItemsSource as IEnumerable<SGOfferingLookupItem>)?.FirstOrDefault(x => x.Id == offering.Id)?.Label ?? $"Offering #{offering.Id}";
        }

        private string SGResolveStudentName(long studentId)
        {
            var student = _sgStudents.FirstOrDefault(x => x.Id == studentId);
            return student == null ? $"Student {studentId}" : $"{student.LastName}, {student.FirstName}";
        }

        private decimal SGResolveWeight(GradeComponentName componentName)
        {
            return _sgGradeComponents.FirstOrDefault(x => x.Name == componentName)?.Weight ?? 0m;
        }

        private static bool SGTryParseGradeRow(SGGradeRow row, out decimal? writtenWorks, out decimal? performanceTasks, out decimal? quarterlyAssessment, out decimal? quarterGrade, out string errorMessage)
        {
            writtenWorks = null; performanceTasks = null; quarterlyAssessment = null; quarterGrade = null; errorMessage = string.Empty;
            if (!SGTryParseScore(row.WrittenWorksText, "Written Works", out writtenWorks, out errorMessage) ||
                !SGTryParseScore(row.PerformanceTasksText, "Performance Tasks", out performanceTasks, out errorMessage) ||
                !SGTryParseScore(row.QuarterlyAssessmentText, "Quarterly Assessment", out quarterlyAssessment, out errorMessage))
                return false;
            quarterGrade = string.IsNullOrWhiteSpace(row.QuarterGradeText) ? null : decimal.Parse(row.QuarterGradeText, NumberStyles.Number, CultureInfo.InvariantCulture);
            return true;
        }

        private static bool SGTryParseScore(string rawValue, string fieldName, out decimal? parsedValue, out string errorMessage)
        {
            parsedValue = null; errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(rawValue)) return true;
            if (!decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var score)) { errorMessage = $"{fieldName} must be a valid number."; return false; }
            if (score < 0m || score > 100m) { errorMessage = $"{fieldName} must be between 0 and 100."; return false; }
            parsedValue = Math.Round(score, 2);
            return true;
        }
    }
}

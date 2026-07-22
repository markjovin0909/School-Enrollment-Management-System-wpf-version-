using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System
{
    public partial class MainWindow
    {
        private const string ReportEnrollment = "Enrollment Report";
        private const string ReportEnrollmentSummary = "Enrollment Summary";
        private const string ReportEnrolledPerGrade = "Enrolled per Grade Level";
        private const string ReportEnrolledPerSection = "Enrolled per Section";
        private const string ReportTeacherAssignments = "Teacher Assignment Report";
        private const string ReportMasterlist = "Masterlist (Section/Class)";
        private const string ReportStudentEnrollmentHistory = "Student Enrollment History";
        private const string ReportSchoolYearComparison = "School Year Comparison Report";
        private const string ReportPrintableEnrollmentForm = "Printable Enrollment Form";
        private const string ReportOfficialEnrollmentStatistics = "Official Enrollment Statistics";
        private const string ReportRunAction = "RUN";
        private const string ReportExportAction = "EXPORT_CSV";
        private const string ReportPresetStateKey = "reports.presetId";

        private bool _suppressReportPresetEvents;
        private List<ReportFilterPreset> _reportPresets = new();

        private void InitializeReportsTab()
        {
            cboReportType.ItemsSource = new[]
            {
                ReportEnrollmentSummary,
                ReportEnrollment,
                ReportEnrolledPerGrade,
                ReportEnrolledPerSection,
                ReportTeacherAssignments,
                ReportMasterlist,
                ReportStudentEnrollmentHistory,
                ReportSchoolYearComparison,
                ReportPrintableEnrollmentForm,
                ReportOfficialEnrollmentStatistics
            };
            cboReportStatus.ItemsSource = new[] { "All", "PENDING", "ENROLLED", "RESERVED", "CANCELLED", "DROPPED", "COMPLETED" };

            var savedType = GetSessionState("reports.type");
            cboReportType.SelectedItem = cboReportType.Items
                .OfType<string>()
                .FirstOrDefault(x => string.Equals(x, savedType, StringComparison.OrdinalIgnoreCase))
                ?? ReportEnrollmentSummary;

            var savedStatus = GetSessionState("reports.status");
            cboReportStatus.SelectedItem = cboReportStatus.Items
                .OfType<string>()
                .FirstOrDefault(x => string.Equals(x, savedStatus, StringComparison.OrdinalIgnoreCase))
                ?? "All";

            cboReportType.SelectionChanged += (_, _) =>
            {
                if (_suppressReportEvents) return;
                SetSessionState("reports.type", cboReportType.SelectedItem?.ToString());
                ApplyReportTypeState();
                LoadReport(recordHistory: false, trigger: "FILTER_CHANGE");
            };
            cboReportSchoolYear.SelectionChanged += (_, _) =>
            {
                if (_suppressReportEvents) return;
                SetSessionStateLong("reports.schoolYearId", (cboReportSchoolYear.SelectedItem as SchoolYear)?.Id);
                LoadReport(recordHistory: false, trigger: "FILTER_CHANGE");
            };
            cboReportGrade.SelectionChanged += (_, _) =>
            {
                if (_suppressReportEvents) return;
                SetSessionStateLong("reports.gradeId", (cboReportGrade.SelectedItem as GradeLevel)?.Id);
                LoadReport(recordHistory: false, trigger: "FILTER_CHANGE");
            };
            cboReportSection.SelectionChanged += (_, _) =>
            {
                if (_suppressReportEvents) return;
                SetSessionStateLong("reports.sectionId", (cboReportSection.SelectedItem as Section)?.Id);
                LoadReport(recordHistory: false, trigger: "FILTER_CHANGE");
            };
            cboReportStatus.SelectionChanged += (_, _) =>
            {
                if (_suppressReportEvents) return;
                SetSessionState("reports.status", cboReportStatus.SelectedItem?.ToString());
                LoadReport(recordHistory: false, trigger: "FILTER_CHANGE");
            };
            cboReportStudent.SelectionChanged += (_, _) =>
            {
                if (_suppressReportEvents) return;
                SetSessionStateLong("reports.studentId", (cboReportStudent.SelectedItem as ReportStudentItem)?.Id);
                LoadReport(recordHistory: false, trigger: "FILTER_CHANGE");
            };
            cboReportPreset.SelectionChanged += (_, _) =>
            {
                if (_suppressReportPresetEvents) return;
                var preset = cboReportPreset.SelectedItem as ReportFilterPreset;
                SetSessionState(ReportPresetStateKey, preset?.Id);
                txtReportPresetName.Text = preset?.Name ?? string.Empty;
            };
            WireGridSortPersistence(gridReports, "reports");

            btnReportLoad.Click += (_, _) => LoadReport(recordHistory: true, trigger: ReportRunAction);
            btnReportExport.Click += (_, _) => ExportReport();
            btnReportPresetApply.Click += (_, _) => ApplySelectedReportPreset(runReport: true);
            btnReportPresetSave.Click += (_, _) => SaveCurrentReportPreset();
            btnReportPresetDelete.Click += (_, _) => DeleteSelectedReportPreset();
            btnReportHistoryRefresh.Click += (_, _) => LoadReportRunHistory();

            LoadReportsLookups();
            LoadReportPresets();
            ApplySelectedReportPreset(runReport: false);
            ApplyReportTypeState();
            LoadReport(recordHistory: false, trigger: "INITIAL_LOAD");
            LoadReportRunHistory();
        }

        private void LoadReportsLookups()
        {
            _suppressReportEvents = true;

            _schoolYears = _schoolYearService.GetAll().OrderByDescending(x => x.Name).ToList();
            _gradeLevels = _gradeLevelService.GetAll().OrderBy(x => x.Code).ToList();
            _sections = _sectionService.GetAll().OrderBy(x => x.Name).ToList();
            _students = _studentService.GetAll().OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToList();
            _teachers = _teacherService.GetAll().ToList();

            cboReportSchoolYear.ItemsSource = _schoolYears.Prepend(new SchoolYear { Id = 0, Name = "All" });
            cboReportSchoolYear.DisplayMemberPath = "Name";
            var schoolYearOptions = cboReportSchoolYear.ItemsSource.Cast<SchoolYear>().ToList();
            var selectedSchoolYear = ResolveById(schoolYearOptions, GetSessionStateLong("reports.schoolYearId"), x => x.Id);
            var defaultSchoolYear = SchoolYearSelectionHelper.ResolveActive(_schoolYears, _schoolYearService);
            var defaultSchoolYearOption = defaultSchoolYear == null
                ? null
                : schoolYearOptions.FirstOrDefault(x => x.Id == defaultSchoolYear.Id);
            cboReportSchoolYear.SelectedItem = selectedSchoolYear ?? defaultSchoolYearOption ?? schoolYearOptions.FirstOrDefault();

            cboReportGrade.ItemsSource = _gradeLevels.Prepend(new GradeLevel { Id = 0, Code = "All", Name = "All" });
            cboReportGrade.DisplayMemberPath = "Code";
            var gradeOptions = cboReportGrade.ItemsSource.Cast<GradeLevel>().ToList();
            var selectedGrade = ResolveById(gradeOptions, GetSessionStateLong("reports.gradeId"), x => x.Id);
            cboReportGrade.SelectedItem = selectedGrade ?? gradeOptions.FirstOrDefault();

            cboReportSection.ItemsSource = _sections.Prepend(new Section { Id = 0, Name = "All" });
            cboReportSection.DisplayMemberPath = "Name";
            var sectionOptions = cboReportSection.ItemsSource.Cast<Section>().ToList();
            var selectedSection = ResolveById(sectionOptions, GetSessionStateLong("reports.sectionId"), x => x.Id);
            cboReportSection.SelectedItem = selectedSection ?? sectionOptions.FirstOrDefault();

            var studentItems = _students
                .Select(x => new ReportStudentItem(x.Id, $"{x.LastName}, {x.FirstName} ({x.Lrn})"))
                .ToList();
            studentItems.Insert(0, new ReportStudentItem(0, "All"));
            cboReportStudent.ItemsSource = studentItems;
            cboReportStudent.DisplayMemberPath = "Label";
            var selectedStudent = ResolveById(studentItems, GetSessionStateLong("reports.studentId"), x => x.Id);
            cboReportStudent.SelectedItem = selectedStudent ?? studentItems.FirstOrDefault();

            SetSessionStateLong("reports.schoolYearId", (cboReportSchoolYear.SelectedItem as SchoolYear)?.Id);
            SetSessionStateLong("reports.gradeId", (cboReportGrade.SelectedItem as GradeLevel)?.Id);
            SetSessionStateLong("reports.sectionId", (cboReportSection.SelectedItem as Section)?.Id);
            SetSessionStateLong("reports.studentId", (cboReportStudent.SelectedItem as ReportStudentItem)?.Id);

            _suppressReportEvents = false;
        }

        private void ApplyReportTypeState()
        {
            var selected = cboReportType.SelectedItem?.ToString() ?? ReportEnrollment;
            var requiresStudent = string.Equals(selected, ReportStudentEnrollmentHistory, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(selected, ReportPrintableEnrollmentForm, StringComparison.OrdinalIgnoreCase);
            cboReportStudent.IsEnabled = requiresStudent;
        }

        private void LoadReportPresets()
        {
            _reportPresets = _reportPresetHistoryService.LoadPresets().ToList();
            _suppressReportPresetEvents = true;
            cboReportPreset.ItemsSource = _reportPresets;
            cboReportPreset.DisplayMemberPath = "Name";

            var selectedPresetId = GetSessionState(ReportPresetStateKey);
            var selectedPreset = _reportPresets.FirstOrDefault(x => string.Equals(x.Id, selectedPresetId, StringComparison.OrdinalIgnoreCase));
            cboReportPreset.SelectedItem = selectedPreset;
            txtReportPresetName.Text = selectedPreset?.Name ?? string.Empty;
            _suppressReportPresetEvents = false;
        }

        private void ApplySelectedReportPreset(bool runReport)
        {
            if (cboReportPreset.SelectedItem is not ReportFilterPreset preset)
            {
                return;
            }

            ApplyReportPreset(preset);
            if (runReport)
            {
                LoadReport(recordHistory: true, trigger: ReportRunAction);
            }
        }

        private void ApplyReportPreset(ReportFilterPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            _suppressReportEvents = true;

            cboReportType.SelectedItem = cboReportType.Items
                .OfType<string>()
                .FirstOrDefault(x => string.Equals(x, preset.ReportType, StringComparison.OrdinalIgnoreCase))
                ?? ReportEnrollmentSummary;

            var schoolYearOptions = cboReportSchoolYear.ItemsSource?.Cast<SchoolYear>().ToList() ?? new List<SchoolYear>();
            var presetSchoolYear = ResolveById(schoolYearOptions, preset.SchoolYearId, x => x.Id);
            var activeSchoolYearOption = SchoolYearSelectionHelper.ResolveActive(_schoolYears, _schoolYearService) is SchoolYear activeYear
                ? schoolYearOptions.FirstOrDefault(x => x.Id == activeYear.Id)
                : null;
            cboReportSchoolYear.SelectedItem = presetSchoolYear ?? activeSchoolYearOption ?? schoolYearOptions.FirstOrDefault();

            var gradeOptions = cboReportGrade.ItemsSource?.Cast<GradeLevel>().ToList() ?? new List<GradeLevel>();
            cboReportGrade.SelectedItem = ResolveById(gradeOptions, preset.GradeId, x => x.Id) ?? gradeOptions.FirstOrDefault();

            var sectionOptions = cboReportSection.ItemsSource?.Cast<Section>().ToList() ?? new List<Section>();
            cboReportSection.SelectedItem = ResolveById(sectionOptions, preset.SectionId, x => x.Id) ?? sectionOptions.FirstOrDefault();

            cboReportStatus.SelectedItem = cboReportStatus.Items
                .OfType<string>()
                .FirstOrDefault(x => string.Equals(x, preset.Status, StringComparison.OrdinalIgnoreCase))
                ?? "All";

            var studentOptions = cboReportStudent.ItemsSource?.Cast<ReportStudentItem>().ToList() ?? new List<ReportStudentItem>();
            cboReportStudent.SelectedItem = ResolveById(studentOptions, preset.StudentId, x => x.Id) ?? studentOptions.FirstOrDefault();

            _suppressReportEvents = false;
            txtReportPresetName.Text = preset.Name;
            SetSessionState(ReportPresetStateKey, preset.Id);
            SetSessionState("reports.type", cboReportType.SelectedItem?.ToString());
            SetSessionStateLong("reports.schoolYearId", (cboReportSchoolYear.SelectedItem as SchoolYear)?.Id);
            SetSessionStateLong("reports.gradeId", (cboReportGrade.SelectedItem as GradeLevel)?.Id);
            SetSessionStateLong("reports.sectionId", (cboReportSection.SelectedItem as Section)?.Id);
            SetSessionState("reports.status", cboReportStatus.SelectedItem?.ToString());
            SetSessionStateLong("reports.studentId", (cboReportStudent.SelectedItem as ReportStudentItem)?.Id);
            ApplyReportTypeState();
        }

        private void SaveCurrentReportPreset()
        {
            var name = (txtReportPresetName.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Enter a preset name first.", "Report Presets", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedPresetId = (cboReportPreset.SelectedItem as ReportFilterPreset)?.Id;
            var preset = BuildCurrentPreset(name, selectedPresetId);
            var saveResult = _reportPresetHistoryService.SavePreset(preset);
            if (!saveResult.Success || saveResult.Data == null)
            {
                MessageBox.Show(saveResult.Message, "Report Presets", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadReportPresets();
            _suppressReportPresetEvents = true;
            cboReportPreset.SelectedItem = _reportPresets.FirstOrDefault(x => string.Equals(x.Id, saveResult.Data.Id, StringComparison.OrdinalIgnoreCase));
            _suppressReportPresetEvents = false;
            SetSessionState(ReportPresetStateKey, saveResult.Data.Id);

            MessageBox.Show("Report preset saved.", "Report Presets", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteSelectedReportPreset()
        {
            if (cboReportPreset.SelectedItem is not ReportFilterPreset preset)
            {
                MessageBox.Show("Select a preset to delete.", "Report Presets", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Delete preset '{preset.Name}'?",
                "Report Presets",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = _reportPresetHistoryService.DeletePreset(preset.Id);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Report Presets", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SetSessionState(ReportPresetStateKey, null);
            LoadReportPresets();
            txtReportPresetName.Text = string.Empty;
            MessageBox.Show("Report preset deleted.", "Report Presets", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private ReportFilterPreset BuildCurrentPreset(string name, string? existingId)
        {
            return new ReportFilterPreset
            {
                Id = existingId ?? string.Empty,
                Name = name,
                ReportType = cboReportType.SelectedItem?.ToString() ?? ReportEnrollmentSummary,
                SchoolYearId = (cboReportSchoolYear.SelectedItem as SchoolYear)?.Id,
                GradeId = (cboReportGrade.SelectedItem as GradeLevel)?.Id,
                SectionId = (cboReportSection.SelectedItem as Section)?.Id,
                Status = cboReportStatus.SelectedItem?.ToString() ?? "All",
                StudentId = (cboReportStudent.SelectedItem as ReportStudentItem)?.Id,
                CreatedByUserId = _currentUser.Id,
                CreatedByUsername = _currentUser.Username
            };
        }

        private void LoadReportRunHistory()
        {
            var history = _reportPresetHistoryService.LoadHistory(40).ToList();

            var table = new DataTable();
            table.Columns.Add("Date");
            table.Columns.Add("Action");
            table.Columns.Add("Report");
            table.Columns.Add("Preset");
            table.Columns.Add("Rows");
            table.Columns.Add("Filters");
            table.Columns.Add("User");
            table.Columns.Add("Result");

            foreach (var entry in history)
            {
                table.Rows.Add(
                    entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    entry.Action,
                    entry.ReportType,
                    string.IsNullOrWhiteSpace(entry.PresetName) ? "-" : entry.PresetName,
                    entry.RowCount,
                    BuildHistoryFilterSummary(entry),
                    string.IsNullOrWhiteSpace(entry.ExecutedByUsername) ? "-" : entry.ExecutedByUsername,
                    entry.Success ? "SUCCESS" : "FAILED");
            }

            gridReportRunHistory.ItemsSource = table.DefaultView;
            txtReportRunHistorySummary.Text = history.Count == 0
                ? "Run History: no report run or export recorded yet."
                : $"Run History: showing {history.Count} latest report action(s).";
        }

        private string BuildHistoryFilterSummary(ReportRunHistoryEntry entry)
        {
            var schoolYearName = entry.SchoolYearId.HasValue && entry.SchoolYearId.Value > 0
                ? (_schoolYears.FirstOrDefault(x => x.Id == entry.SchoolYearId.Value)?.Name ?? $"SY#{entry.SchoolYearId.Value}")
                : "All SY";
            var gradeName = entry.GradeId.HasValue && entry.GradeId.Value > 0
                ? (_gradeLevels.FirstOrDefault(x => x.Id == entry.GradeId.Value)?.Code ?? $"G#{entry.GradeId.Value}")
                : "All Grade";
            var sectionName = entry.SectionId.HasValue && entry.SectionId.Value > 0
                ? (_sections.FirstOrDefault(x => x.Id == entry.SectionId.Value)?.Name ?? $"Section#{entry.SectionId.Value}")
                : "All Section";
            var status = string.IsNullOrWhiteSpace(entry.Status) ? "All" : entry.Status;
            return $"{schoolYearName} | {gradeName} | {sectionName} | {status}";
        }

        private void AppendReportRunHistory(string action, bool success, string notes, string? exportFilePath)
        {
            var historyEntry = new ReportRunHistoryEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Action = action,
                ReportType = cboReportType.SelectedItem?.ToString() ?? ReportEnrollmentSummary,
                PresetName = (cboReportPreset.SelectedItem as ReportFilterPreset)?.Name ?? string.Empty,
                SchoolYearId = (cboReportSchoolYear.SelectedItem as SchoolYear)?.Id,
                GradeId = (cboReportGrade.SelectedItem as GradeLevel)?.Id,
                SectionId = (cboReportSection.SelectedItem as Section)?.Id,
                Status = cboReportStatus.SelectedItem?.ToString() ?? "All",
                StudentId = (cboReportStudent.SelectedItem as ReportStudentItem)?.Id,
                RowCount = _reportsTable?.Rows?.Count ?? 0,
                Success = success,
                Notes = notes,
                ExportFilePath = exportFilePath ?? string.Empty,
                ExecutedByUserId = _currentUser.Id,
                ExecutedByUsername = _currentUser.Username
            };

            _reportPresetHistoryService.AppendHistory(historyEntry);
        }

        private void LoadReport(bool recordHistory, string trigger)
        {
            try
            {
                var reportType = cboReportType.SelectedItem?.ToString() ?? ReportEnrollment;
                _reportsTable = reportType switch
                {
                    ReportEnrollmentSummary => BuildEnrollmentSummaryReport(),
                    ReportEnrolledPerGrade => BuildEnrolledPerGradeReport(),
                    ReportEnrolledPerSection => BuildEnrolledPerSectionReport(),
                    ReportTeacherAssignments => BuildTeacherAssignmentReport(),
                    ReportMasterlist => BuildMasterlistReport(),
                    ReportStudentEnrollmentHistory => BuildStudentEnrollmentHistoryReport(),
                    ReportSchoolYearComparison => BuildSchoolYearComparisonReport(),
                    ReportPrintableEnrollmentForm => BuildPrintableEnrollmentFormReport(),
                    ReportOfficialEnrollmentStatistics => BuildOfficialEnrollmentStatisticsReport(),
                    _ => BuildEnrollmentReport()
                };

                gridReports.ItemsSource = _reportsTable.DefaultView;
                ApplyGridSort("reports", _reportsTable.DefaultView);

                if (recordHistory)
                {
                    AppendReportRunHistory(trigger, success: true, notes: $"Loaded {_reportsTable.Rows.Count} row(s).", exportFilePath: null);
                    LoadReportRunHistory();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load report: {ex.Message}", "Reports", MessageBoxButton.OK, MessageBoxImage.Error);
                if (recordHistory)
                {
                    AppendReportRunHistory(trigger, success: false, notes: ex.Message, exportFilePath: null);
                    LoadReportRunHistory();
                }
            }
        }

        private DataTable BuildEnrollmentReport()
        {
            var enrollments = GetFilteredEnrollments().ToList();
            var sectionCounts = enrollments.GroupBy(e => e.SectionId).ToDictionary(g => g.Key, g => g.Count());

            var table = new DataTable();
            table.Columns.Add("Student");
            table.Columns.Add("LRN");
            table.Columns.Add("GradeLevel");
            table.Columns.Add("Section");
            table.Columns.Add("SchoolYear");
            table.Columns.Add("Status");
            table.Columns.Add("Approval");
            table.Columns.Add("Type");
            table.Columns.Add("Waitlist");
            table.Columns.Add("EnrolledAt");
            table.Columns.Add("Capacity");
            table.Columns.Add("CapacityUsed");
            table.Columns.Add("Adviser");

            foreach (var e in enrollments)
            {
                var student = _students.FirstOrDefault(s => s.Id == e.StudentId);
                var section = _sections.FirstOrDefault(s => s.Id == e.SectionId);
                var grade = _gradeLevels.FirstOrDefault(g => g.Id == e.GradeLevelId);
                var schoolYear = _schoolYears.FirstOrDefault(s => s.Id == e.SchoolYearId);
                var adviser = _teachers.FirstOrDefault(t => t.Id == section?.AdviserTeacherId);

                var capacity = section?.Capacity ?? 0;
                var count = section != null && sectionCounts.TryGetValue(section.Id, out var value) ? value : 0;
                var used = capacity > 0 ? (count * 100m / capacity) : 0m;

                table.Rows.Add(
                    student == null ? string.Empty : $"{student.LastName}, {student.FirstName}",
                    student?.Lrn ?? string.Empty,
                    grade?.Code ?? string.Empty,
                    section?.Name ?? string.Empty,
                    schoolYear?.Name ?? string.Empty,
                    e.Status.ToString(),
                    e.ApprovalStatus.ToString(),
                    e.EnrollmentType,
                    e.WaitlistPosition.HasValue ? $"#{e.WaitlistPosition.Value}" : string.Empty,
                    e.EnrolledAt.ToString("yyyy-MM-dd"),
                    capacity == 0 ? string.Empty : capacity.ToString(),
                    capacity == 0 ? string.Empty : $"{used:0.##}%",
                    adviser == null ? string.Empty : $"{adviser.LastName}, {adviser.FirstName}");
            }

            return table;
        }

        private DataTable BuildEnrollmentSummaryReport()
        {
            var enrollments = GetFilteredEnrollments().ToList();
            var table = new DataTable();
            table.Columns.Add("Metric");
            table.Columns.Add("Value");
            table.Rows.Add("Total Enrollments", enrollments.Count);
            table.Rows.Add("Pending", enrollments.Count(e => e.Status == EnrollmentStatus.PENDING));
            table.Rows.Add("Enrolled", enrollments.Count(e => e.Status == EnrollmentStatus.ENROLLED));
            table.Rows.Add("Reserved", enrollments.Count(e => e.Status == EnrollmentStatus.RESERVED));
            table.Rows.Add("Cancelled", enrollments.Count(e => e.Status == EnrollmentStatus.CANCELLED));
            table.Rows.Add("Dropped", enrollments.Count(e => e.Status == EnrollmentStatus.DROPPED));
            table.Rows.Add("Completed", enrollments.Count(e => e.Status == EnrollmentStatus.COMPLETED));
            return table;
        }

        private DataTable BuildEnrolledPerGradeReport()
        {
            var enrollments = GetFilteredEnrollments().Where(e => e.Status == EnrollmentStatus.ENROLLED).ToList();
            var table = new DataTable();
            table.Columns.Add("Grade");
            table.Columns.Add("Enrolled Students");

            var rows = enrollments
                .GroupBy(e => e.GradeLevelId)
                .Select(g => new
                {
                    Grade = _gradeLevels.FirstOrDefault(x => x.Id == g.Key)?.Code ?? $"Grade {g.Key}",
                    Count = g.Count()
                })
                .OrderBy(x => x.Grade)
                .ToList();

            foreach (var row in rows)
            {
                table.Rows.Add(row.Grade, row.Count);
            }

            return table;
        }

        private DataTable BuildEnrolledPerSectionReport()
        {
            var enrollments = GetFilteredEnrollments().Where(e => e.Status == EnrollmentStatus.ENROLLED).ToList();
            var table = new DataTable();
            table.Columns.Add("Section");
            table.Columns.Add("Grade");
            table.Columns.Add("Enrolled Students");

            var rows = enrollments
                .GroupBy(e => e.SectionId)
                .Select(g =>
                {
                    var section = _sections.FirstOrDefault(x => x.Id == g.Key);
                    var grade = _gradeLevels.FirstOrDefault(x => x.Id == (section?.GradeLevelId ?? 0));
                    return new
                    {
                        Section = section?.Name ?? $"Section {g.Key}",
                        Grade = grade?.Code ?? string.Empty,
                        Count = g.Count()
                    };
                })
                .OrderBy(x => x.Grade)
                .ThenBy(x => x.Section)
                .ToList();

            foreach (var row in rows)
            {
                table.Rows.Add(row.Section, row.Grade, row.Count);
            }

            return table;
        }

        private DataTable BuildTeacherAssignmentReport()
        {
            var syId = cboReportSchoolYear.SelectedItem is SchoolYear sy && sy.Id != 0 ? sy.Id : (long?)null;
            var gradeId = cboReportGrade.SelectedItem is GradeLevel gr && gr.Id != 0 ? gr.Id : (long?)null;
            var sectionId = cboReportSection.SelectedItem is Section sec && sec.Id != 0 ? sec.Id : (long?)null;

            var table = new DataTable();
            table.Columns.Add("Teacher");
            table.Columns.Add("Subject");
            table.Columns.Add("Section");
            table.Columns.Add("Grade");
            table.Columns.Add("School Year");
            table.Columns.Add("Offering Status");

            var offerings = _classOfferingService.GetAll().ToList();
            foreach (var offering in offerings)
            {
                if (syId.HasValue && offering.SchoolYearId != syId.Value) continue;
                if (sectionId.HasValue && offering.SectionId != sectionId.Value) continue;

                var section = _sections.FirstOrDefault(s => s.Id == offering.SectionId);
                if (gradeId.HasValue && section?.GradeLevelId != gradeId.Value) continue;

                var teacher = _teachers.FirstOrDefault(t => t.Id == offering.TeacherId);
                var subject = _subjectService.GetById(offering.SubjectId);
                var grade = _gradeLevels.FirstOrDefault(g => g.Id == (section?.GradeLevelId ?? 0));
                var schoolYear = _schoolYears.FirstOrDefault(y => y.Id == offering.SchoolYearId);

                table.Rows.Add(
                    teacher == null ? "(Unassigned)" : $"{teacher.LastName}, {teacher.FirstName}",
                    subject?.Title ?? string.Empty,
                    section?.Name ?? string.Empty,
                    grade?.Code ?? string.Empty,
                    schoolYear?.Name ?? string.Empty,
                    offering.Status.ToString());
            }

            return table;
        }

        private DataTable BuildStudentEnrollmentHistoryReport()
        {
            var table = new DataTable();
            table.Columns.Add("Student");
            table.Columns.Add("LRN");
            table.Columns.Add("School Year");
            table.Columns.Add("Grade");
            table.Columns.Add("Section");
            table.Columns.Add("Status");
            table.Columns.Add("Enrolled At");

            var selectedStudentId = cboReportStudent.SelectedItem is ReportStudentItem studentItem && studentItem.Id != 0 ? studentItem.Id : (long?)null;
            var enrollments = _enrollmentService.GetAll().ToList();
            if (selectedStudentId.HasValue)
            {
                enrollments = enrollments.Where(e => e.StudentId == selectedStudentId.Value).ToList();
            }

            foreach (var e in enrollments.OrderByDescending(e => e.EnrolledAt))
            {
                var student = _students.FirstOrDefault(s => s.Id == e.StudentId);
                var schoolYear = _schoolYears.FirstOrDefault(s => s.Id == e.SchoolYearId);
                var grade = _gradeLevels.FirstOrDefault(g => g.Id == e.GradeLevelId);
                var section = _sections.FirstOrDefault(s => s.Id == e.SectionId);

                table.Rows.Add(
                    student == null ? string.Empty : $"{student.LastName}, {student.FirstName}",
                    student?.Lrn ?? string.Empty,
                    schoolYear?.Name ?? string.Empty,
                    grade?.Code ?? string.Empty,
                    section?.Name ?? string.Empty,
                    e.Status.ToString(),
                    e.EnrolledAt.ToString("yyyy-MM-dd"));
            }

            return table;
        }

        private DataTable BuildMasterlistReport()
        {
            var enrollments = GetFilteredEnrollments().ToList();
            var table = new DataTable();
            table.Columns.Add("No");
            table.Columns.Add("Student");
            table.Columns.Add("LRN");
            table.Columns.Add("Sex");
            table.Columns.Add("GradeLevel");
            table.Columns.Add("Section");
            table.Columns.Add("SchoolYear");
            table.Columns.Add("Status");
            table.Columns.Add("Adviser");

            var rows = enrollments
                .Select(e =>
                {
                    var student = _students.FirstOrDefault(s => s.Id == e.StudentId);
                    var section = _sections.FirstOrDefault(s => s.Id == e.SectionId);
                    var grade = _gradeLevels.FirstOrDefault(g => g.Id == e.GradeLevelId);
                    var schoolYear = _schoolYears.FirstOrDefault(s => s.Id == e.SchoolYearId);
                    var adviser = _teachers.FirstOrDefault(t => t.Id == section?.AdviserTeacherId);

                    return new
                    {
                        StudentSort = student == null ? string.Empty : $"{student.LastName}|{student.FirstName}",
                        StudentName = student == null ? string.Empty : $"{student.LastName}, {student.FirstName}",
                        student?.Lrn,
                        Sex = student?.Sex?.ToString() ?? string.Empty,
                        Grade = grade?.Code ?? string.Empty,
                        Section = section?.Name ?? string.Empty,
                        SchoolYear = schoolYear?.Name ?? string.Empty,
                        Status = e.Status.ToString(),
                        Adviser = adviser == null ? string.Empty : $"{adviser.LastName}, {adviser.FirstName}"
                    };
                })
                .OrderBy(x => x.Section)
                .ThenBy(x => x.StudentSort)
                .ToList();

            var index = 1;
            foreach (var row in rows)
            {
                table.Rows.Add(index++, row.StudentName, row.Lrn ?? string.Empty, row.Sex, row.Grade, row.Section, row.SchoolYear, row.Status, row.Adviser);
            }

            return table;
        }

        private DataTable BuildSchoolYearComparisonReport()
        {
            var table = new DataTable();
            table.Columns.Add("School Year");
            table.Columns.Add("Total");
            table.Columns.Add("Enrolled");
            table.Columns.Add("Pending");
            table.Columns.Add("Reserved");
            table.Columns.Add("Cancelled");
            table.Columns.Add("Dropped");
            table.Columns.Add("Completed");
            table.Columns.Add("Delta Enrolled vs Previous");

            var years = _schoolYears
                .OrderBy(x => x.StartDate ?? DateTime.MinValue)
                .ThenBy(x => x.Name)
                .ToList();
            if (years.Count == 0)
            {
                return table;
            }

            var allEnrollments = _enrollmentService.GetAll().ToList();
            int? previousEnrolled = null;
            foreach (var year in years)
            {
                var yearEnrollments = allEnrollments.Where(x => x.SchoolYearId == year.Id).ToList();
                var enrolled = yearEnrollments.Count(x => x.Status == EnrollmentStatus.ENROLLED);
                var delta = previousEnrolled.HasValue ? enrolled - previousEnrolled.Value : 0;

                table.Rows.Add(
                    year.Name,
                    yearEnrollments.Count,
                    enrolled,
                    yearEnrollments.Count(x => x.Status == EnrollmentStatus.PENDING),
                    yearEnrollments.Count(x => x.Status == EnrollmentStatus.RESERVED),
                    yearEnrollments.Count(x => x.Status == EnrollmentStatus.CANCELLED),
                    yearEnrollments.Count(x => x.Status == EnrollmentStatus.DROPPED),
                    yearEnrollments.Count(x => x.Status == EnrollmentStatus.COMPLETED),
                    previousEnrolled.HasValue ? (delta >= 0 ? $"+{delta}" : delta.ToString()) : "N/A");

                previousEnrolled = enrolled;
            }

            return table;
        }

        private DataTable BuildPrintableEnrollmentFormReport()
        {
            var table = new DataTable();
            table.Columns.Add("Field");
            table.Columns.Add("Value");

            var selectedStudentId = cboReportStudent.SelectedItem is ReportStudentItem item && item.Id != 0 ? item.Id : (long?)null;
            if (!selectedStudentId.HasValue)
            {
                table.Rows.Add("Notice", "Select a student to generate the printable enrollment form.");
                return table;
            }

            var schoolYearId = cboReportSchoolYear.SelectedItem is SchoolYear sy && sy.Id != 0 ? sy.Id : (long?)null;
            var enrollments = _enrollmentService.GetAll().Where(x => x.StudentId == selectedStudentId.Value).ToList();
            if (schoolYearId.HasValue)
            {
                enrollments = enrollments.Where(x => x.SchoolYearId == schoolYearId.Value).ToList();
            }

            var enrollment = enrollments.OrderByDescending(x => x.EnrolledAt).ThenByDescending(x => x.Id).FirstOrDefault();
            if (enrollment == null)
            {
                table.Rows.Add("Notice", "No enrollment record found for the selected student.");
                return table;
            }

            var student = _students.FirstOrDefault(x => x.Id == enrollment.StudentId);
            var schoolYear = _schoolYears.FirstOrDefault(x => x.Id == enrollment.SchoolYearId);
            var grade = _gradeLevels.FirstOrDefault(x => x.Id == enrollment.GradeLevelId);
            var section = _sections.FirstOrDefault(x => x.Id == enrollment.SectionId);
            var identity = _schoolSettingService.GetPrintIdentity();

            table.Rows.Add("Print Header 1", identity.PrintHeaderLine1);
            table.Rows.Add("Print Header 2", identity.PrintHeaderLine2);
            table.Rows.Add("School Name", identity.SchoolName);
            table.Rows.Add("School ID", identity.SchoolCode);
            table.Rows.Add("School Address", identity.SchoolAddress);
            table.Rows.Add("Principal", identity.PrincipalName);
            table.Rows.Add("Student Name", student == null ? string.Empty : $"{student.LastName}, {student.FirstName}");
            table.Rows.Add("Student Number", student?.StudentNumber ?? string.Empty);
            table.Rows.Add("LRN", student?.Lrn ?? string.Empty);
            table.Rows.Add("School Year", schoolYear?.Name ?? string.Empty);
            table.Rows.Add("Grade Level", string.IsNullOrWhiteSpace(grade?.Code) ? grade?.Name ?? string.Empty : $"{grade.Code} - {grade.Name}");
            table.Rows.Add("Section", section?.Name ?? string.Empty);
            table.Rows.Add("Enrollment Status", enrollment.Status.ToString());
            table.Rows.Add("Approval Status", enrollment.ApprovalStatus.ToString());
            table.Rows.Add("Enrollment Type", enrollment.EnrollmentType);
            table.Rows.Add("Waitlist Position", enrollment.WaitlistPosition.HasValue ? $"#{enrollment.WaitlistPosition.Value}" : "N/A");
            table.Rows.Add("Enrolled Date", enrollment.EnrolledAt.ToString("yyyy-MM-dd"));
            table.Rows.Add("Notes", enrollment.Notes ?? string.Empty);

            return table;
        }

        private DataTable BuildOfficialEnrollmentStatisticsReport()
        {
            var enrollments = GetFilteredEnrollments().ToList();
            var table = new DataTable();
            table.Columns.Add("Category");
            table.Columns.Add("Metric");
            table.Columns.Add("Value");

            var total = enrollments.Count;
            table.Rows.Add("Overall", "Total Enrollments", total);
            table.Rows.Add("Overall", "Approved", enrollments.Count(x => x.ApprovalStatus == EnrollmentApprovalStatus.APPROVED));
            table.Rows.Add("Overall", "Waitlisted", enrollments.Count(x => x.Status == EnrollmentStatus.RESERVED));
            table.Rows.Add("Overall", "Pending", enrollments.Count(x => x.Status == EnrollmentStatus.PENDING));
            table.Rows.Add("Overall", "Completion Rate", total == 0 ? "0.00%" : $"{(enrollments.Count(x => x.Status == EnrollmentStatus.COMPLETED) * 100.0 / total):0.00}%");

            var byGrade = enrollments
                .GroupBy(x => x.GradeLevelId)
                .Select(g =>
                {
                    var grade = _gradeLevels.FirstOrDefault(x => x.Id == g.Key);
                    var label = grade == null
                        ? $"Grade {g.Key}"
                        : string.IsNullOrWhiteSpace(grade.Code) ? grade.Name : $"{grade.Code} - {grade.Name}";
                    return new { Grade = label, Total = g.Count(), Enrolled = g.Count(x => x.Status == EnrollmentStatus.ENROLLED) };
                })
                .OrderBy(x => x.Grade)
                .ToList();
            foreach (var grade in byGrade)
            {
                table.Rows.Add("By Grade", grade.Grade, $"{grade.Enrolled}/{grade.Total} enrolled");
            }

            var byType = enrollments
                .GroupBy(x => string.IsNullOrWhiteSpace(x.EnrollmentType) ? "UNSPECIFIED" : x.EnrollmentType.ToUpperInvariant())
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .OrderBy(x => x.Type)
                .ToList();
            foreach (var type in byType)
            {
                table.Rows.Add("By Student Type", type.Type, type.Count);
            }

            return table;
        }

        private IEnumerable<Enrollment> GetFilteredEnrollments()
        {
            var schoolYearId = cboReportSchoolYear.SelectedItem is SchoolYear sy && sy.Id != 0 ? sy.Id : (long?)null;
            var gradeId = cboReportGrade.SelectedItem is GradeLevel gl && gl.Id != 0 ? gl.Id : (long?)null;
            var sectionId = cboReportSection.SelectedItem is Section sec && sec.Id != 0 ? sec.Id : (long?)null;
            var statusFilter = cboReportStatus.SelectedItem?.ToString();

            var enrollments = _enrollmentService.GetAll();
            if (schoolYearId.HasValue) enrollments = enrollments.Where(e => e.SchoolYearId == schoolYearId.Value);
            if (gradeId.HasValue) enrollments = enrollments.Where(e => e.GradeLevelId == gradeId.Value);
            if (sectionId.HasValue) enrollments = enrollments.Where(e => e.SectionId == sectionId.Value);
            if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                enrollments = enrollments.Where(e => string.Equals(e.Status.ToString(), statusFilter, StringComparison.OrdinalIgnoreCase));
            }

            return enrollments;
        }

        private void ExportReport()
        {
            if (_reportsTable.Rows.Count == 0)
            {
                MessageBox.Show("No data to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var exportedPath = CsvExportService.SaveDataTable(_reportsTable, ResolveDefaultReportFileName());
            if (string.IsNullOrWhiteSpace(exportedPath))
            {
                return;
            }

            AppendReportRunHistory(
                ReportExportAction,
                success: true,
                notes: $"Exported {_reportsTable.Rows.Count} row(s).",
                exportFilePath: exportedPath);
            LoadReportRunHistory();
        }

        private string ResolveDefaultReportFileName()
        {
            return (cboReportType.SelectedItem?.ToString() ?? ReportEnrollment) switch
            {
                ReportEnrollmentSummary => "enrollment_summary.csv",
                ReportEnrolledPerGrade => "enrolled_per_grade.csv",
                ReportEnrolledPerSection => "enrolled_per_section.csv",
                ReportTeacherAssignments => "teacher_assignment_report.csv",
                ReportMasterlist => "section_masterlist.csv",
                ReportStudentEnrollmentHistory => "student_enrollment_history.csv",
                ReportSchoolYearComparison => "school_year_comparison.csv",
                ReportPrintableEnrollmentForm => "printable_enrollment_form.csv",
                ReportOfficialEnrollmentStatistics => "official_enrollment_statistics.csv",
                _ => "enrollment_report.csv"
            };
        }

        private sealed record ReportStudentItem(long Id, string Label);
    }
}

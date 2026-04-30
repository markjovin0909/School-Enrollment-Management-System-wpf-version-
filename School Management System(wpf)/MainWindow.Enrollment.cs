using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.Views;

namespace School_Management_System
{
    public partial class MainWindow
    {
        private static readonly ReasonPromptWindow.ReasonOption[] EnrollmentGovernanceReasonOptions =
        {
            new("POLICY_DECISION", "Policy decision"),
            new("REQUIREMENT_REVIEW", "Requirement review"),
            new("CAPACITY_MANAGEMENT", "Capacity management"),
            new("CORRECTION_REQUEST", "Correction request"),
            new("STATUS_ADJUSTMENT", "Status adjustment"),
            new("OTHER", "Other (details required)")
        };

        private readonly Dictionary<long, DateTime?> _enrollmentSnapshotUpdatedAtByStudentId = new();
        private DateTime? _selectedEnrollmentSnapshotUpdatedAtUtc;
        private string _selectedEnrollmentSnapshotStatus = "NOT_ENROLLED";
        private string _selectedEnrollmentSnapshotApproval = "-";

        private void InitializeEnrollmentTab()
        {
            cboEnrollStatusChange.ItemsSource = Enum.GetValues(typeof(EnrollmentStatus));
            cboEnrollStatusFilter.ItemsSource = new[] { "All Statuses", "NOT_ENROLLED", "PENDING", "ENROLLED", "RESERVED", "CANCELLED", "DROPPED", "COMPLETED", "TRANSFERRED_OUT" };
            cboEnrollStatusFilter.SelectedIndex = 0;
            txtEnrollSearch.Text = GetSessionState("enrollment.search");

            var savedStatusChange = GetSessionState("enrollment.statusChange");
            if (Enum.TryParse<EnrollmentStatus>(savedStatusChange, true, out var parsedStatusChange))
            {
                cboEnrollStatusChange.SelectedItem = parsedStatusChange;
            }
            else
            {
                cboEnrollStatusChange.SelectedItem = EnrollmentStatus.PENDING;
            }

            cboEnrollSchoolYear.SelectionChanged += (_, _) =>
            {
                if (_suppressEnrollmentEvents) return;
                SetSessionStateLong("enrollment.schoolYearId", (cboEnrollSchoolYear.SelectedItem as SchoolYear)?.Id);
                BindEnrollmentSections();
                LoadEnrollmentStudents();
            };
            cboEnrollGrade.SelectionChanged += (_, _) =>
            {
                if (_suppressEnrollmentEvents) return;
                SetSessionStateLong("enrollment.gradeId", (cboEnrollGrade.SelectedItem as GradeLevel)?.Id);
                BindEnrollmentSections();
                LoadEnrollmentStudents();
            };
            cboEnrollSection.SelectionChanged += (_, _) =>
            {
                if (_suppressEnrollmentEvents) return;
                SetSessionStateLong("enrollment.sectionId", (cboEnrollSection.SelectedItem as Section)?.Id);
                LoadEnrollmentStudents();
            };
            cboEnrollCurriculum.SelectionChanged += (_, _) =>
            {
                if (_suppressEnrollmentEvents) return;
                SetSessionStateLong("enrollment.curriculumId", (cboEnrollCurriculum.SelectedItem as Curriculum)?.Id);
                LoadEnrollmentStudents();
            };
            cboEnrollStatusFilter.SelectionChanged += (_, _) =>
            {
                if (_suppressEnrollmentEvents) return;
                SetSessionState("enrollment.statusFilter", cboEnrollStatusFilter.SelectedItem?.ToString());
                LoadEnrollmentStudents();
            };
            txtEnrollSearch.TextChanged += (_, _) =>
            {
                if (_suppressEnrollmentEvents) return;
                SetSessionState("enrollment.search", txtEnrollSearch.Text);
                LoadEnrollmentStudents();
            };
            cboEnrollStatusChange.SelectionChanged += (_, _) =>
            {
                if (_suppressEnrollmentEvents) return;
                SetSessionState("enrollment.statusChange", cboEnrollStatusChange.SelectedItem?.ToString());
            };

            gridEnrollmentStudents.AutoGeneratingColumn += GridEnrollmentStudents_AutoGeneratingColumn;
            gridEnrollmentStudents.LoadingRow += GridEnrollmentStudents_LoadingRow;
            gridEnrollmentStudents.SelectionChanged += GridEnrollmentStudents_SelectionChanged;
            WireGridSortPersistence(gridEnrollmentStudents, "enrollment");

            btnEnrollRefresh.Click += (_, _) => RefreshEnrollmentTab();
            btnEnrollSubmit.Click += (_, _) => SubmitEnrollment();
            btnEnrollTransfer.Click += (_, _) => TransferEnrollment();
            btnEnrollApprove.Click += (_, _) => ApproveEnrollment();
            btnEnrollReturn.Click += (_, _) => ReturnEnrollmentForCorrection();
            btnEnrollCancel.Click += (_, _) => CancelEnrollment();
            btnEnrollDrop.Click += (_, _) => DropEnrollment();
            btnEnrollPromote.Click += (_, _) => PromoteWaitlist();
            btnEnrollSetStatus.Click += (_, _) => SetEnrollmentStatus();
            btnEnrollNextReview.Click += (_, _) => SelectNextEnrollmentForReview();
            btnEnrollReloadSelection.Click += (_, _) => ReloadSelectedEnrollmentContext();

            RefreshEnrollmentTab();
        }

        private void RefreshEnrollmentTab()
        {
            _schoolYears = _schoolYearService.GetAll().Where(x => !x.IsArchived).ToList();
            _gradeLevels = _gradeLevelService.GetAll().ToList();
            _sections = _sectionService.GetAll().Where(x => !x.IsArchived).ToList();
            _curricula = _curriculumService.GetAll().ToList();
            _students = _studentService.GetAll().ToList();

            _suppressEnrollmentEvents = true;
            cboEnrollSchoolYear.ItemsSource = _schoolYears;
            cboEnrollSchoolYear.DisplayMemberPath = "Name";
            var savedSchoolYear = ResolveById(_schoolYears, GetSessionStateLong("enrollment.schoolYearId"), x => x.Id);
            cboEnrollSchoolYear.SelectedItem = savedSchoolYear ?? _schoolYears.FirstOrDefault(x => x.Status == SchoolYearStatus.ACTIVE) ?? _schoolYears.FirstOrDefault();

            cboEnrollGrade.ItemsSource = _gradeLevels;
            cboEnrollGrade.DisplayMemberPath = "Code";
            var savedGrade = ResolveById(_gradeLevels, GetSessionStateLong("enrollment.gradeId"), x => x.Id);
            cboEnrollGrade.SelectedItem = savedGrade ?? _gradeLevels.FirstOrDefault();

            cboEnrollCurriculum.ItemsSource = _curricula;
            cboEnrollCurriculum.DisplayMemberPath = "Name";
            var savedCurriculum = ResolveById(_curricula, GetSessionStateLong("enrollment.curriculumId"), x => x.Id);
            cboEnrollCurriculum.SelectedItem = savedCurriculum ?? _curricula.FirstOrDefault();

            var savedStatusFilter = GetSessionState("enrollment.statusFilter");
            cboEnrollStatusFilter.SelectedItem = cboEnrollStatusFilter.Items
                .OfType<string>()
                .FirstOrDefault(x => string.Equals(x, savedStatusFilter, StringComparison.OrdinalIgnoreCase))
                ?? "All Statuses";

            BindEnrollmentSections();
            _suppressEnrollmentEvents = false;

            SetSessionStateLong("enrollment.schoolYearId", (cboEnrollSchoolYear.SelectedItem as SchoolYear)?.Id);
            SetSessionStateLong("enrollment.gradeId", (cboEnrollGrade.SelectedItem as GradeLevel)?.Id);
            SetSessionStateLong("enrollment.curriculumId", (cboEnrollCurriculum.SelectedItem as Curriculum)?.Id);

            LoadEnrollmentStudents();
        }

        private void BindEnrollmentSections()
        {
            var schoolYear = cboEnrollSchoolYear.SelectedItem as SchoolYear;
            var grade = cboEnrollGrade.SelectedItem as GradeLevel;
            var sections = _sections
                .Where(s => (schoolYear == null || s.SchoolYearId == schoolYear.Id) &&
                            (grade == null || s.GradeLevelId == grade.Id))
                .OrderBy(s => s.Name)
                .ToList();

            cboEnrollSection.ItemsSource = sections;
            cboEnrollSection.DisplayMemberPath = "Name";
            var savedSection = ResolveById(sections, GetSessionStateLong("enrollment.sectionId"), x => x.Id);
            cboEnrollSection.SelectedItem = savedSection ?? sections.FirstOrDefault();
            SetSessionStateLong("enrollment.sectionId", (cboEnrollSection.SelectedItem as Section)?.Id);

            LoadEnrollmentClassView(schoolYear?.Id, (cboEnrollSection.SelectedItem as Section)?.Id);
        }

        private void LoadEnrollmentStudents(long? preferredStudentId = null)
        {
            var schoolYear = cboEnrollSchoolYear.SelectedItem as SchoolYear;
            var section = cboEnrollSection.SelectedItem as Section;
            var search = (txtEnrollSearch.Text ?? string.Empty).Trim();
            var statusFilter = (cboEnrollStatusFilter.SelectedItem as string ?? "All Statuses").Trim();
            var enrollments = _enrollmentService.GetAll().ToList();
            _enrollmentQueueSlaPolicy = _enrollmentQueueSlaService.LoadPolicy();
            var enrollmentByStudent = schoolYear == null
                ? new Dictionary<long, Enrollment>()
                : enrollments
                    .Where(x => x.SchoolYearId == schoolYear.Id)
                    .GroupBy(x => x.StudentId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EnrolledAt).ThenByDescending(x => x.Id).First());
            _enrollmentSnapshotUpdatedAtByStudentId.Clear();
            _enrollmentQueueSeverityByStudentId.Clear();

            _enrollmentTable = new DataTable();
            _enrollmentTable.Columns.Add("Id", typeof(long));
            _enrollmentTable.Columns.Add("Student No");
            _enrollmentTable.Columns.Add("LRN");
            _enrollmentTable.Columns.Add("Student");
            _enrollmentTable.Columns.Add("Preferred Grade");
            _enrollmentTable.Columns.Add("Curriculum");
            _enrollmentTable.Columns.Add("Requirements");
            _enrollmentTable.Columns.Add("Enrollment Status");
            _enrollmentTable.Columns.Add("Approval");
            _enrollmentTable.Columns.Add("Type");
            _enrollmentTable.Columns.Add("Waitlist");
            _enrollmentTable.Columns.Add("Queue Age");
            _enrollmentTable.Columns.Add("SLA");

            var referenceUtc = DateTime.UtcNow;

            foreach (var student in _students)
            {
                var enrollment = enrollmentByStudent.TryGetValue(student.Id, out var existing) ? existing : null;
                var statusText = enrollment?.Status.ToString() ?? "NOT_ENROLLED";
                var approvalText = enrollment?.ApprovalStatus.ToString() ?? "-";
                var typeText = enrollment?.EnrollmentType ?? "-";
                var waitlistText = enrollment?.WaitlistPosition.HasValue == true ? $"#{enrollment.WaitlistPosition.Value}" : "-";
                var slaEvaluation = _enrollmentQueueSlaService.Evaluate(enrollment, referenceUtc, _enrollmentQueueSlaPolicy);
                var requirementSnapshot = _requirementChecklistService.BuildForStudent(
                    student.Id,
                    _studentRequirementService.GetAll().Where(x => x.StudentId == student.Id).ToList());
                var gradeLabel = _gradeLevels.FirstOrDefault(x => x.Id == student.PreferredGradeLevelId)?.Code
                    ?? _gradeLevels.FirstOrDefault(x => x.Id == student.PreferredGradeLevelId)?.Name
                    ?? "(Not set)";
                var curriculumLabel = _curricula.FirstOrDefault(x => x.Id == student.PreferredCurriculumId)?.Name ?? "(Not set)";
                _enrollmentSnapshotUpdatedAtByStudentId[student.Id] = enrollment?.UpdatedAt;
                _enrollmentQueueSeverityByStudentId[student.Id] = slaEvaluation.Severity;

                if (!string.Equals(statusFilter, "All Statuses", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(statusText, statusFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var matches = (student.StudentNumber ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                  (student.Lrn ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                  ($"{student.LastName}, {student.FirstName}").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                  statusText.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                  gradeLabel.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                  curriculumLabel.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                  slaEvaluation.Label.Contains(search, StringComparison.OrdinalIgnoreCase);
                    if (!matches)
                    {
                        continue;
                    }
                }

                if (section != null && enrollment != null && enrollment.SectionId != section.Id)
                {
                    // keep visible if searching explicitly; otherwise filter to selected section context
                    if (string.IsNullOrWhiteSpace(search))
                    {
                        continue;
                    }
                }

                _enrollmentTable.Rows.Add(
                    student.Id,
                    student.StudentNumber,
                    student.Lrn,
                    $"{student.LastName}, {student.FirstName}",
                    gradeLabel,
                    curriculumLabel,
                    requirementSnapshot.MissingCount == 0 ? "Complete" : $"{requirementSnapshot.MissingCount} missing",
                    statusText,
                    approvalText,
                    typeText,
                    waitlistText,
                    slaEvaluation.AgeDisplay,
                    slaEvaluation.Label);
            }

            gridEnrollmentStudents.ItemsSource = _enrollmentTable.DefaultView;
            ApplyGridSort("enrollment", _enrollmentTable.DefaultView);
            UpdateEnrollmentQueueInfo();
            if (preferredStudentId.HasValue)
            {
                SelectEnrollmentStudent(preferredStudentId.Value);
            }
            else if (_selectedEnrollmentStudentId.HasValue)
            {
                SelectEnrollmentStudent(_selectedEnrollmentStudentId.Value);
            }
            else
            {
                UpdateEnrollmentReviewContext(null);
            }

            LoadEnrollmentClassView(schoolYear?.Id, section?.Id);
        }

        private void SelectEnrollmentStudent(long studentId)
        {
            foreach (var item in gridEnrollmentStudents.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == studentId)
                {
                    gridEnrollmentStudents.SelectedItem = item;
                    gridEnrollmentStudents.ScrollIntoView(item);
                    return;
                }
            }
        }

        private void GridEnrollmentStudents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridEnrollmentStudents.SelectedItem is not DataRowView row)
            {
                _selectedEnrollmentStudentId = null;
                _selectedEnrollmentSnapshotUpdatedAtUtc = null;
                _selectedEnrollmentSnapshotStatus = "NOT_ENROLLED";
                _selectedEnrollmentSnapshotApproval = "-";
                UpdateEnrollmentReviewContext(null);
                return;
            }

            _selectedEnrollmentStudentId = row.Row.Field<long>("Id");
            _selectedEnrollmentSnapshotUpdatedAtUtc = _enrollmentSnapshotUpdatedAtByStudentId.TryGetValue(_selectedEnrollmentStudentId.Value, out var updatedAt)
                ? updatedAt
                : null;
            _selectedEnrollmentSnapshotStatus = row.Row["Enrollment Status"]?.ToString() ?? "NOT_ENROLLED";
            _selectedEnrollmentSnapshotApproval = row.Row["Approval"]?.ToString() ?? "-";
            UpdateEnrollmentReviewContext(GetSelectedEnrollment());
        }

        private void GridEnrollmentStudents_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Id")
            {
                e.Cancel = true;
                return;
            }

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Student No",
                "LRN",
                "Student",
                "Preferred Grade",
                "Curriculum",
                "Requirements",
                "Enrollment Status",
                "Approval",
                "Queue Age",
                "SLA"
            };

            if (!allowed.Contains(e.PropertyName))
            {
                e.Cancel = true;
                return;
            }

            if (e.PropertyName == "Student No")
            {
                e.Column.Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 0;
                return;
            }

            if (e.PropertyName == "LRN")
            {
                e.Column.Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 1;
                return;
            }

            if (e.PropertyName == "Student")
            {
                e.Column.Width = new DataGridLength(2.1, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 2;
                return;
            }

            if (e.PropertyName == "Preferred Grade")
            {
                e.Column.Width = new DataGridLength(0.9, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 3;
                return;
            }

            if (e.PropertyName == "Curriculum")
            {
                e.Column.Width = new DataGridLength(1.1, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 4;
                return;
            }

            if (e.PropertyName == "Requirements")
            {
                e.Column.Width = new DataGridLength(0.95, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 5;
                return;
            }

            if (e.PropertyName == "Enrollment Status")
            {
                e.Column.Width = new DataGridLength(1.3, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 6;
                return;
            }

            if (e.PropertyName == "Approval")
            {
                e.Column.Width = new DataGridLength(0.9, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 7;
                return;
            }

            if (e.PropertyName == "Queue Age")
            {
                e.Column.Width = new DataGridLength(0.95, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 8;
                return;
            }

            if (e.PropertyName == "SLA")
            {
                e.Column.Width = new DataGridLength(0.8, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 9;
            }
        }

        private void GridEnrollmentStudents_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is not DataRowView row)
            {
                return;
            }

            if (!_enrollmentQueueSeverityByStudentId.TryGetValue(row.Row.Field<long>("Id"), out var severity))
            {
                return;
            }

            var rowBackground = severity switch
            {
                EnrollmentQueueSlaSeverity.Critical => (Brush?)new SolidColorBrush(Color.FromRgb(255, 244, 244)),
                EnrollmentQueueSlaSeverity.Warning => (Brush?)new SolidColorBrush(Color.FromRgb(255, 249, 236)),
                _ => null
            };

            if (rowBackground == null)
            {
                e.Row.ClearValue(Control.BackgroundProperty);
                return;
            }

            e.Row.Background = rowBackground;
        }

        private void LoadEnrollmentClassView(long? schoolYearId, long? sectionId)
        {
            var table = new DataTable();
            table.Columns.Add("Subject");
            table.Columns.Add("Grade");
            table.Columns.Add("Section");
            table.Columns.Add("Teacher");
            table.Columns.Add("Room");
            table.Columns.Add("Schedule");
            table.Columns.Add("Slots");
            table.Columns.Add("Status");

            if (schoolYearId.HasValue && sectionId.HasValue)
            {
                var offerings = _classOfferingService.GetAll()
                    .Where(x => x.SchoolYearId == schoolYearId.Value && x.SectionId == sectionId.Value)
                    .ToList();
                var schedules = _classScheduleService.GetAll().ToList();
                var section = _sections.FirstOrDefault(x => x.Id == sectionId.Value);
                var enrolledCount = _enrollmentService.GetAll()
                    .Count(x => x.SchoolYearId == schoolYearId.Value && x.SectionId == sectionId.Value && x.Status == EnrollmentStatus.ENROLLED);

                foreach (var offering in offerings)
                {
                    var subject = _subjectService.GetById(offering.SubjectId)?.Title ?? string.Empty;
                    var teacher = offering.TeacherId.HasValue ? _teacherService.GetById(offering.TeacherId.Value) : null;
                    var teacherName = teacher == null ? "Unassigned" : $"{teacher.LastName}, {teacher.FirstName}";
                    var grade = section?.GradeLevelId is long gradeId
                        ? _gradeLevels.FirstOrDefault(x => x.Id == gradeId)?.Code ?? _gradeLevels.FirstOrDefault(x => x.Id == gradeId)?.Name ?? string.Empty
                        : string.Empty;
                    var offeringSchedules = schedules.Where(x => x.ClassOfferingId == offering.Id).ToList();
                    var scheduleLabel = offeringSchedules.Count == 0
                        ? "No schedule"
                        : string.Join(", ", offeringSchedules.Select(x => $"{ResolveDayOfWeekLabel(x.DayOfWeek)} {x.StartTime:hh\\:mm}-{x.EndTime:hh\\:mm}"));
                    var roomLabel = !string.IsNullOrWhiteSpace(offering.Room) ? offering.Room : "TBD";
                    var slotsLabel = section?.Capacity.HasValue == true
                        ? $"{Math.Max(section.Capacity.Value - enrolledCount, 0)} / {section.Capacity.Value} open"
                        : "Open";
                    table.Rows.Add(subject, grade, section?.Name ?? string.Empty, teacherName, roomLabel, scheduleLabel, slotsLabel, offering.Status.ToString());
                }
            }

            gridEnrollmentClasses.ItemsSource = table.DefaultView;
        }

        private Enrollment? GetSelectedEnrollment()
        {
            if (!_selectedEnrollmentStudentId.HasValue || cboEnrollSchoolYear.SelectedItem is not SchoolYear schoolYear)
            {
                return null;
            }

            return _enrollmentService.GetAll()
                .FirstOrDefault(x => x.SchoolYearId == schoolYear.Id && x.StudentId == _selectedEnrollmentStudentId.Value);
        }

        private void UpdateEnrollmentQueueInfo()
        {
            var total = _enrollmentTable?.Rows?.Count ?? 0;
            var pending = 0;
            var reserved = 0;
            var warning = 0;
            var critical = 0;
            if (_enrollmentTable?.Rows != null)
            {
                foreach (DataRow row in _enrollmentTable.Rows)
                {
                    var status = row["Enrollment Status"]?.ToString() ?? string.Empty;
                    if (string.Equals(status, EnrollmentStatus.PENDING.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        pending++;
                    }
                    else if (string.Equals(status, EnrollmentStatus.RESERVED.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        reserved++;
                    }

                    var sla = row["SLA"]?.ToString() ?? string.Empty;
                    if (string.Equals(sla, "WARNING", StringComparison.OrdinalIgnoreCase))
                    {
                        warning++;
                    }
                    else if (string.Equals(sla, "CRITICAL", StringComparison.OrdinalIgnoreCase))
                    {
                        critical++;
                    }
                }
            }

            txtEnrollmentQueueInfo.Text = $"{total} student(s) in queue | {pending} pending | {reserved} waitlisted | {warning} warning | {critical} critical";
        }

        private void UpdateEnrollmentReviewContext(Enrollment? enrollment)
        {
            var student = _selectedEnrollmentStudentId.HasValue
                ? _students.FirstOrDefault(x => x.Id == _selectedEnrollmentStudentId.Value)
                : null;

            if (student == null)
            {
                txtEnrollReviewStudent.Text = "No student selected.";
                txtEnrollReviewState.Text = "Status: - | Approval: -";
                txtEnrollReviewMeta.Text = "Select a queue item to review details.";
                txtEnrollRequirementSummary.Text = "No student selected.";
                txtEnrollRecommendedSection.Text = "Recommended section will appear after selecting a student and matching the active grade and school year.";
                enrollRequirementChecklistPanel.Items = Array.Empty<RequirementChecklistItem>();
                enrollRequirementChecklistPanel.SummaryText = "No student selected.";
                HideEnrollmentStaleWarning();
                return;
            }

            txtEnrollReviewStudent.Text = $"{student.LastName}, {student.FirstName} ({student.StudentNumber})";
            var requirementSnapshot = UpdateEnrollmentRequirementChecklist(student.Id);
            var preferredGrade = _gradeLevels.FirstOrDefault(x => x.Id == student.PreferredGradeLevelId)?.Code
                ?? _gradeLevels.FirstOrDefault(x => x.Id == student.PreferredGradeLevelId)?.Name
                ?? "(Not set)";
            var preferredCurriculum = _curricula.FirstOrDefault(x => x.Id == student.PreferredCurriculumId)?.Name ?? "(Not set)";
            txtEnrollReviewMeta.Text = $"{preferredGrade} / {preferredCurriculum}";
            txtEnrollRequirementSummary.Text = requirementSnapshot.MissingCount == 0
                ? "All required documents complete."
                : $"{requirementSnapshot.MissingCount} required document(s) missing.";
            txtEnrollRecommendedSection.Text = ResolveRecommendedSectionText(student);

            if (enrollment == null)
            {
                txtEnrollReviewState.Text = $"Status: {_selectedEnrollmentSnapshotStatus} | Approval: {_selectedEnrollmentSnapshotApproval}";
                HideEnrollmentStaleWarning();
                return;
            }

            var waitlistText = enrollment.WaitlistPosition.HasValue ? $" | Waitlist: #{enrollment.WaitlistPosition.Value}" : string.Empty;
            txtEnrollReviewState.Text = $"Status: {enrollment.Status} | Approval: {enrollment.ApprovalStatus}{waitlistText}";
            txtEnrollRecommendedSection.Text = $"{ResolveRecommendedSectionText(student)} | Current record type: {enrollment.EnrollmentType} | Last Updated: {enrollment.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
            HideEnrollmentStaleWarning();
        }

        private RequirementChecklistSnapshot UpdateEnrollmentRequirementChecklist(long studentId)
        {
            var requirements = _studentRequirementService.GetAll().Where(x => x.StudentId == studentId).ToList();
            var snapshot = _requirementChecklistService.BuildForStudent(studentId, requirements);
            enrollRequirementChecklistPanel.Items = snapshot.Items;
            enrollRequirementChecklistPanel.SummaryText = snapshot.SummaryText;
            return snapshot;
        }

        private void HideEnrollmentStaleWarning()
        {
            enrollStaleBanner.Visibility = Visibility.Collapsed;
            txtEnrollStaleMessage.Text = string.Empty;
        }

        private void ShowEnrollmentStaleWarning(string message)
        {
            txtEnrollStaleMessage.Text = message;
            enrollStaleBanner.Visibility = Visibility.Visible;
        }

        private void ReloadSelectedEnrollmentContext()
        {
            if (!_selectedEnrollmentStudentId.HasValue)
            {
                MessageBox.Show("Select an enrollment queue row first.", "Enrollment", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
        }

        private void SelectNextEnrollmentForReview()
        {
            if (gridEnrollmentStudents.Items.Count == 0)
            {
                MessageBox.Show("No enrollment queue rows available.", "Enrollment", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var startIndex = gridEnrollmentStudents.SelectedIndex < 0 ? -1 : gridEnrollmentStudents.SelectedIndex;
            var total = gridEnrollmentStudents.Items.Count;
            for (var offset = 1; offset <= total; offset++)
            {
                var index = (startIndex + offset + total) % total;
                if (gridEnrollmentStudents.Items[index] is not DataRowView row)
                {
                    continue;
                }

                var status = row.Row["Enrollment Status"]?.ToString() ?? string.Empty;
                var approval = row.Row["Approval"]?.ToString() ?? string.Empty;
                var shouldReview =
                    string.Equals(status, EnrollmentStatus.PENDING.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(status, EnrollmentStatus.RESERVED.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(approval, EnrollmentApprovalStatus.PENDING.ToString(), StringComparison.OrdinalIgnoreCase);

                if (!shouldReview)
                {
                    continue;
                }

                gridEnrollmentStudents.SelectedIndex = index;
                gridEnrollmentStudents.ScrollIntoView(gridEnrollmentStudents.Items[index]);
                return;
            }

            MessageBox.Show("No pending or waitlisted queue items found.", "Enrollment", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string ResolveRecommendedSectionText(Student student)
        {
            var schoolYearId = (cboEnrollSchoolYear.SelectedItem as SchoolYear)?.Id;
            if (!schoolYearId.HasValue || !student.PreferredGradeLevelId.HasValue)
            {
                return "Recommended section unavailable because the student preference or active school year context is incomplete.";
            }

            var recommended = _sections
                .Where(x => x.SchoolYearId == schoolYearId.Value &&
                            x.GradeLevelId == student.PreferredGradeLevelId.Value &&
                            !x.IsArchived)
                .OrderBy(x => x.Name)
                .FirstOrDefault();

            if (recommended == null)
            {
                return "No recommended section found for the selected school year and preferred grade.";
            }

            var curriculumLabel = student.PreferredCurriculumId.HasValue
                ? _curricula.FirstOrDefault(x => x.Id == student.PreferredCurriculumId.Value)?.Name ?? "(Not set)"
                : "(Not set)";
            return $"Recommended section: {recommended.Name} for {curriculumLabel}.";
        }

        private static string ResolveDayOfWeekLabel(byte dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Mon",
                2 => "Tue",
                3 => "Wed",
                4 => "Thu",
                5 => "Fri",
                6 => "Sat",
                7 => "Sun",
                _ => "Day"
            };
        }

        private bool EnsureEnrollmentFreshForDecision(Enrollment currentEnrollment, string actionName)
        {
            if (!_selectedEnrollmentSnapshotUpdatedAtUtc.HasValue)
            {
                HideEnrollmentStaleWarning();
                return true;
            }

            var latest = _enrollmentService.GetById(currentEnrollment.Id);
            if (latest == null)
            {
                ShowEnrollmentStaleWarning("Enrollment record no longer exists. Reload queue before proceeding.");
                TryRaiseEnrollmentConcurrencyException(
                    currentEnrollment,
                    "Enrollment record no longer exists while processing decision.");
                MessageBox.Show("Enrollment record no longer exists. Reload the queue.", actionName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (latest.UpdatedAt != _selectedEnrollmentSnapshotUpdatedAtUtc.Value)
            {
                ShowEnrollmentStaleWarning("Selected enrollment changed after queue load. Click 'Reload Selected' and review latest data.");
                TryRaiseEnrollmentConcurrencyException(
                    currentEnrollment,
                    "Selected enrollment changed after queue load.");
                MessageBox.Show(
                    "The selected enrollment was modified by a newer update. Reload selected row before continuing.",
                    actionName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            HideEnrollmentStaleWarning();
            return true;
        }

        private void TryRaiseEnrollmentConcurrencyException(Enrollment enrollment, string summary)
        {
            try
            {
                _exceptionQueueService.Raise(new ExceptionQueueCreateRequest
                {
                    Category = "CONCURRENCY_CONFLICT",
                    SourceModule = "Enrollment.ReviewWorkbench",
                    Entity = "enrollments",
                    EntityId = enrollment.Id,
                    Severity = ExceptionQueueSeverity.WARNING,
                    Summary = summary,
                    Details = $"EnrollmentId={enrollment.Id}; StudentId={enrollment.StudentId}; SchoolYearId={enrollment.SchoolYearId}",
                    CorrelationId = CorrelationContext.CurrentId
                });
            }
            catch
            {
                // Exception queue capture should not block enrollment flow feedback.
            }
        }

        private bool ConfirmWithSummary(EnrollmentDraft draft, long? existingEnrollmentId, string title, out EnrollmentValidationSummary? summary)
        {
            summary = null;
            var summaryResult = _enrollmentService.BuildValidationSummary(draft, existingEnrollmentId);
            if (!summaryResult.Success || summaryResult.Data == null)
            {
                MessageBox.Show(BuildDetailedError(summaryResult.Message, summaryResult.Errors), title, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            summary = summaryResult.Data;
            txtEnrollValidationSummary.Text = summary.ToDisplayText();
            var confirm = MessageBox.Show(
                $"{summary.ToDisplayText()}\n\nProceed?",
                title,
                MessageBoxButton.OKCancel,
                summary.CanSubmit ? MessageBoxImage.Information : MessageBoxImage.Warning);
            return confirm == MessageBoxResult.OK;
        }

        private void SubmitEnrollment()
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var draft = BuildEnrollmentDraft(_selectedEnrollmentStudentId, ((SchoolYear?)cboEnrollSchoolYear.SelectedItem)?.Id ?? 0L, ((Section?)cboEnrollSection.SelectedItem)?.Id ?? 0L, ((Curriculum?)cboEnrollCurriculum.SelectedItem)?.Id ?? 0L);
            if (draft == null)
            {
                MessageBox.Show("Select student, school year, section, and curriculum first.", "Enrollment", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ConfirmWithSummary(draft, null, "Submit Enrollment Request", out _))
            {
                return;
            }

            var result = _enrollmentService.SubmitEnrollmentRequest(draft);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(BuildDetailedError(result.Message, result.Errors), "Enrollment", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AuditTrailService.Log("CREATE", "enrollments", result.Data.Id, null, result.Data);
            MessageBox.Show(result.Message, "Enrollment", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
        }

        private void TransferEnrollment()
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var existing = GetSelectedEnrollment();
            if (existing == null)
            {
                MessageBox.Show("No enrollment found to update.", "Transfer / Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!EnsureEnrollmentFreshForDecision(existing, "Transfer / Update"))
            {
                return;
            }

            var draft = BuildEnrollmentDraft(_selectedEnrollmentStudentId, existing.SchoolYearId, ((Section?)cboEnrollSection.SelectedItem)?.Id ?? 0L, ((Curriculum?)cboEnrollCurriculum.SelectedItem)?.Id ?? 0L);
            if (draft == null)
            {
                MessageBox.Show("Select section and curriculum.", "Transfer / Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ConfirmWithSummary(draft, existing.Id, existing.SectionId != draft.SectionId ? "Review Section Transfer" : "Review Enrollment Update", out _))
            {
                return;
            }

            var result = _enrollmentService.SubmitEnrollmentRequest(draft, existing.Id);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(BuildDetailedError(result.Message, result.Errors), "Transfer / Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AuditTrailService.Log("UPDATE", "enrollments", result.Data.Id, null, result.Data);
            MessageBox.Show(result.Message, "Transfer / Update", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
        }

        private void ApproveEnrollment()
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var existing = GetSelectedEnrollment();
            if (existing == null)
            {
                MessageBox.Show("No enrollment found for approval.", "Approval", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!EnsureEnrollmentFreshForDecision(existing, "Approval"))
            {
                return;
            }

            var draft = new EnrollmentDraft
            {
                SchoolYearId = existing.SchoolYearId,
                StudentId = existing.StudentId,
                SectionId = existing.SectionId,
                CurriculumId = existing.CurriculumId,
                EnrollmentType = existing.EnrollmentType,
                Notes = existing.Notes
            };

            if (!ConfirmWithSummary(draft, existing.Id, "Final Review Before Approval", out _))
            {
                return;
            }

            var reason = PromptEnrollmentActionReason(
                "Approve Enrollment",
                "Provide the reason for approving this enrollment.");
            if (reason == null)
            {
                return;
            }

            var oldData = new
            {
                PreviousStatus = existing.Status.ToString(),
                PreviousApprovalStatus = existing.ApprovalStatus.ToString(),
                existing.WaitlistPosition,
                existing.SectionId
            };

            var result = _enrollmentService.ApproveEnrollment(existing.Id);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(BuildDetailedError(result.Message, result.Errors), "Approval", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AuditTrailService.Log("APPROVE", "enrollments", result.Data.Id, oldData, new
            {
                EnrollmentId = result.Data.Id,
                PreviousStatus = oldData.PreviousStatus,
                NewStatus = result.Data.Status.ToString(),
                PreviousApprovalStatus = oldData.PreviousApprovalStatus,
                NewApprovalStatus = result.Data.ApprovalStatus.ToString(),
                result.Data.SectionId,
                result.Data.WaitlistPosition,
                reason.ReasonCode,
                reason.ReasonText
            });
            MessageBox.Show(result.Message, "Approval", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
            SelectNextEnrollmentForReview();
        }

        private void ReturnEnrollmentForCorrection()
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var existing = GetSelectedEnrollment();
            if (existing == null)
            {
                MessageBox.Show("No enrollment found.", "Return for Correction", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!EnsureEnrollmentFreshForDecision(existing, "Return for Correction"))
            {
                return;
            }

            var reason = PromptEnrollmentActionReason(
                "Return for Correction",
                "Provide the reason for returning this enrollment for correction.");
            if (reason == null)
            {
                return;
            }

            var oldData = new
            {
                PreviousStatus = existing.Status.ToString(),
                PreviousApprovalStatus = existing.ApprovalStatus.ToString(),
                existing.WaitlistPosition,
                existing.SectionId
            };

            var result = _enrollmentService.ReturnForCorrection(existing.Id);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(BuildDetailedError(result.Message, result.Errors), "Return for Correction", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AuditTrailService.Log("RETURN_FOR_CORRECTION", "enrollments", result.Data.Id, oldData, new
            {
                EnrollmentId = result.Data.Id,
                PreviousStatus = oldData.PreviousStatus,
                NewStatus = result.Data.Status.ToString(),
                PreviousApprovalStatus = oldData.PreviousApprovalStatus,
                NewApprovalStatus = result.Data.ApprovalStatus.ToString(),
                result.Data.SectionId,
                result.Data.WaitlistPosition,
                reason.ReasonCode,
                reason.ReasonText
            });
            MessageBox.Show(result.Message, "Return for Correction", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
            SelectNextEnrollmentForReview();
        }

        private void CancelEnrollment()
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var existing = GetSelectedEnrollment();
            if (existing == null)
            {
                MessageBox.Show("No enrollment found.", "Cancel Enrollment", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!EnsureEnrollmentFreshForDecision(existing, "Cancel Enrollment"))
            {
                return;
            }

            var confirm = MessageBox.Show("Cancel this enrollment request?", "Cancel Enrollment", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var reason = PromptEnrollmentActionReason(
                "Cancel Enrollment",
                "Provide the reason for canceling this enrollment request.");
            if (reason == null)
            {
                return;
            }

            var oldData = new
            {
                PreviousStatus = existing.Status.ToString(),
                PreviousApprovalStatus = existing.ApprovalStatus.ToString(),
                existing.WaitlistPosition,
                existing.SectionId
            };

            var result = _enrollmentService.SetStatus(existing.Id, EnrollmentStatus.CANCELLED);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(BuildDetailedError(result.Message, result.Errors), "Cancel Enrollment", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AuditTrailService.Log("CANCEL", "enrollments", result.Data.Id, oldData, new
            {
                EnrollmentId = result.Data.Id,
                PreviousStatus = oldData.PreviousStatus,
                NewStatus = result.Data.Status.ToString(),
                PreviousApprovalStatus = oldData.PreviousApprovalStatus,
                NewApprovalStatus = result.Data.ApprovalStatus.ToString(),
                result.Data.SectionId,
                result.Data.WaitlistPosition,
                reason.ReasonCode,
                reason.ReasonText
            });
            MessageBox.Show(result.Message, "Cancel Enrollment", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
            SelectNextEnrollmentForReview();
        }

        private void DropEnrollment()
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var existing = GetSelectedEnrollment();
            if (existing == null)
            {
                MessageBox.Show("No enrollment found.", "Drop", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!EnsureEnrollmentFreshForDecision(existing, "Drop"))
            {
                return;
            }

            var targetStatus = existing.Status is EnrollmentStatus.PENDING or EnrollmentStatus.RESERVED
                ? EnrollmentStatus.CANCELLED
                : EnrollmentStatus.DROPPED;

            var confirm = MessageBox.Show(
                $"Set enrollment status to {targetStatus}?",
                "Drop",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var reason = PromptEnrollmentActionReason(
                "Drop Enrollment",
                $"Provide the reason for setting this enrollment to {targetStatus}.");
            if (reason == null)
            {
                return;
            }

            var oldData = new
            {
                PreviousStatus = existing.Status.ToString(),
                PreviousApprovalStatus = existing.ApprovalStatus.ToString(),
                existing.WaitlistPosition,
                existing.SectionId
            };

            var result = _enrollmentService.SetStatus(existing.Id, targetStatus);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(BuildDetailedError(result.Message, result.Errors), "Drop", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AuditTrailService.Log("DROP", "enrollments", result.Data.Id, oldData, new
            {
                EnrollmentId = result.Data.Id,
                PreviousStatus = oldData.PreviousStatus,
                NewStatus = result.Data.Status.ToString(),
                PreviousApprovalStatus = oldData.PreviousApprovalStatus,
                NewApprovalStatus = result.Data.ApprovalStatus.ToString(),
                result.Data.SectionId,
                result.Data.WaitlistPosition,
                reason.ReasonCode,
                reason.ReasonText
            });
            MessageBox.Show(result.Message, "Drop", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
            SelectNextEnrollmentForReview();
        }

        private void PromoteWaitlist()
        {
            using var correlationScope = CorrelationContext.BeginScope();
            if (cboEnrollSchoolYear.SelectedItem is not SchoolYear schoolYear || cboEnrollSection.SelectedItem is not Section section)
            {
                MessageBox.Show("Select school year and section first.", "Waitlist", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var reason = PromptEnrollmentActionReason(
                "Promote Waitlist",
                "Provide the reason for promoting waitlisted enrollments.");
            if (reason == null)
            {
                return;
            }

            var result = _enrollmentService.PromoteWaitlist(schoolYear.Id, section.Id);
            if (!result.Success)
            {
                MessageBox.Show(BuildDetailedError(result.Message, result.Errors), "Waitlist", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AuditTrailService.Log("PROMOTE_WAITLIST", "enrollments", null, null, new
            {
                SchoolYearId = schoolYear.Id,
                SectionId = section.Id,
                Promoted = result.Data,
                reason.ReasonCode,
                reason.ReasonText
            });
            MessageBox.Show(result.Message, "Waitlist", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
        }

        private void SetEnrollmentStatus()
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var existing = GetSelectedEnrollment();
            if (existing == null)
            {
                MessageBox.Show("No enrollment found.", "Status Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!EnsureEnrollmentFreshForDecision(existing, "Status Update"))
            {
                return;
            }

            if (cboEnrollStatusChange.SelectedItem is not EnrollmentStatus status)
            {
                MessageBox.Show("Select a target status.", "Status Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (existing.Status == status)
            {
                MessageBox.Show("Enrollment is already in the selected status.", "Status Update", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var reason = PromptEnrollmentActionReason(
                "Status Update",
                $"Provide the reason for changing status from {existing.Status} to {status}.");
            if (reason == null)
            {
                return;
            }

            var oldData = new
            {
                PreviousStatus = existing.Status.ToString(),
                PreviousApprovalStatus = existing.ApprovalStatus.ToString(),
                existing.WaitlistPosition,
                existing.SectionId
            };

            var result = _enrollmentService.SetStatus(existing.Id, status);
            if (!result.Success || result.Data == null)
            {
                MessageBox.Show(BuildDetailedError(result.Message, result.Errors), "Status Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AuditTrailService.Log("SET_STATUS", "enrollments", result.Data.Id, oldData, new
            {
                EnrollmentId = result.Data.Id,
                PreviousStatus = oldData.PreviousStatus,
                NewStatus = result.Data.Status.ToString(),
                PreviousApprovalStatus = oldData.PreviousApprovalStatus,
                NewApprovalStatus = result.Data.ApprovalStatus.ToString(),
                result.Data.SectionId,
                result.Data.WaitlistPosition,
                reason.ReasonCode,
                reason.ReasonText
            });
            MessageBox.Show(result.Message, "Status Update", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
            SelectNextEnrollmentForReview();
        }

        private EnrollmentActionReason? PromptEnrollmentActionReason(string actionTitle, string prompt)
        {
            var dialog = new ReasonPromptWindow(
                $"{actionTitle} Reason",
                prompt,
                EnrollmentGovernanceReasonOptions)
            {
                Owner = this
            };

            if (dialog.ShowDialog() != true)
            {
                return null;
            }

            return new EnrollmentActionReason(dialog.SelectedReasonCode, dialog.ReasonDetail);
        }

        private sealed class EnrollmentActionReason
        {
            public EnrollmentActionReason(string reasonCode, string reasonText)
            {
                ReasonCode = reasonCode;
                ReasonText = reasonText;
            }

            public string ReasonCode { get; }
            public string ReasonText { get; }
        }
    }
}



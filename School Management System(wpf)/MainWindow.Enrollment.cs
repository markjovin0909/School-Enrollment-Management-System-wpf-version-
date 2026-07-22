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

        private sealed class EnrollmentSubjectOption
        {
            public long SchoolYearId { get; init; }
            public string SchoolYearName { get; init; } = string.Empty;
            public long GradeLevelId { get; init; }
            public string GradeDisplay { get; init; } = string.Empty;
            public long SectionId { get; init; }
            public string SectionName { get; init; } = string.Empty;
            public long SubjectId { get; init; }
            public string SubjectDisplay { get; init; } = string.Empty;
            public int? Capacity { get; init; }
            public int EnrolledCount { get; init; }
            public int AvailableSeats { get; init; }
            public string CapacityDisplay { get; init; } = string.Empty;
            public string AvailableSeatsDisplay { get; init; } = string.Empty;
            public string CurriculumDisplay { get; init; } = string.Empty;
            public string PlacementStatus { get; init; } = string.Empty;
        }

        private sealed class EnrollmentActionReason
        {
            public string ReasonCode { get; init; } = string.Empty;
            public string ReasonText { get; init; } = string.Empty;
        }

        private void InitializeEnrollmentTab()
        {
            cboEnrollStatusChange.ItemsSource = Enum.GetValues(typeof(EnrollmentStatus));
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
                SyncPlacementSelectionFromSection();
                UpdateEnrollmentRecommendedOfferingText();
            };
            cboEnrollSection.SelectionChanged += (_, _) =>
            {
                if (_suppressEnrollmentEvents) return;
                SetSessionStateLong("enrollment.sectionId", (cboEnrollSection.SelectedItem as Section)?.Id);
                SyncPlacementSelectionFromSection();
                UpdateEnrollmentRecommendedOfferingText();
            };
            cboEnrollCurriculum.SelectionChanged += (_, _) =>
            {
                if (_suppressEnrollmentEvents) return;
                SetSessionStateLong("enrollment.curriculumId", (cboEnrollCurriculum.SelectedItem as Curriculum)?.Id);
                UpdateEnrollmentReviewContext(GetSelectedEnrollment());
                RefreshSectionSubjectGrid();
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
            gridEnrollmentClasses.SelectionChanged += GridEnrollmentClasses_SelectionChanged;
            WireGridSortPersistence(gridEnrollmentStudents, "enrollment");

            btnEnrollRefresh.Click += (_, _) => RefreshEnrollmentTab();
            btnEnrollRefreshPlacement.Click += (_, _) => RefreshSectionSubjectGrid();
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
            _cachedEnrollments = _enrollmentService.GetAll().ToList();
            _enrollmentQueueSlaPolicy = _enrollmentQueueSlaService.LoadPolicy();

            _suppressEnrollmentEvents = true;

            cboEnrollSchoolYear.ItemsSource = _schoolYears;
            cboEnrollSchoolYear.DisplayMemberPath = "Name";
            var savedSchoolYear = ResolveById(_schoolYears, GetSessionStateLong("enrollment.schoolYearId"), x => x.Id);
            cboEnrollSchoolYear.SelectedItem = savedSchoolYear ?? SchoolYearSelectionHelper.ResolveActive(_schoolYears, _schoolYearService);

            cboEnrollCurriculum.ItemsSource = _curricula;
            cboEnrollCurriculum.DisplayMemberPath = "Name";
            var savedCurriculum = ResolveById(_curricula, GetSessionStateLong("enrollment.curriculumId"), x => x.Id);
            cboEnrollCurriculum.SelectedItem = savedCurriculum ?? _curricula.FirstOrDefault(x => x.IsActive) ?? _curricula.FirstOrDefault();

            _gradeLevels = _schoolSettingService.OrderGradeLevelsByDefaultScope(_gradeLevels);
            cboEnrollGrade.ItemsSource = _gradeLevels;
            cboEnrollGrade.DisplayMemberPath = "Code";
            var savedGrade = ResolveById(_gradeLevels, GetSessionStateLong("enrollment.gradeId"), x => x.Id);
            var preferredGradeId = _schoolSettingService.GetPrimaryDefaultGradeLevelId();
            var preferredGrade = preferredGradeId.HasValue
                ? _gradeLevels.FirstOrDefault(x => x.Id == preferredGradeId.Value)
                : null;
            cboEnrollGrade.SelectedItem = savedGrade ?? preferredGrade ?? _gradeLevels.FirstOrDefault();

            BindEnrollmentSections();

            _suppressEnrollmentEvents = false;

            SetSessionStateLong("enrollment.schoolYearId", (cboEnrollSchoolYear.SelectedItem as SchoolYear)?.Id);
            SetSessionStateLong("enrollment.curriculumId", (cboEnrollCurriculum.SelectedItem as Curriculum)?.Id);
            SetSessionStateLong("enrollment.gradeId", (cboEnrollGrade.SelectedItem as GradeLevel)?.Id);
            SetSessionStateLong("enrollment.sectionId", (cboEnrollSection.SelectedItem as Section)?.Id);

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

            _suppressEnrollmentEvents = true;
            cboEnrollSection.ItemsSource = sections;
            cboEnrollSection.DisplayMemberPath = "Name";
            var savedSection = ResolveById(sections, GetSessionStateLong("enrollment.sectionId"), x => x.Id);
            cboEnrollSection.SelectedItem = savedSection ?? sections.FirstOrDefault();
            _suppressEnrollmentEvents = false;

            SetSessionStateLong("enrollment.sectionId", (cboEnrollSection.SelectedItem as Section)?.Id);
            RefreshSectionSubjectGrid();
        }

        private void RefreshSectionSubjectGrid()
        {
            var schoolYear = cboEnrollSchoolYear.SelectedItem as SchoolYear;
            var section = cboEnrollSection.SelectedItem as Section;
            var curriculum = cboEnrollCurriculum.SelectedItem as Curriculum;
            _enrollmentPlacementOptions.Clear();

            if (schoolYear != null && section != null)
            {
                var grade = _gradeLevels.FirstOrDefault(x => x.Id == section.GradeLevelId);
                var enrolledCount = _cachedEnrollments.Count(x =>
                    x.SchoolYearId == schoolYear.Id &&
                    x.SectionId == section.Id &&
                    x.Status == EnrollmentStatus.ENROLLED);
                var availableSeats = section.Capacity.HasValue
                    ? Math.Max(section.Capacity.Value - enrolledCount, 0)
                    : int.MaxValue;

                var offerings = _classOfferingService.GetAll()
                    .Where(x => x.SchoolYearId == schoolYear.Id && x.SectionId == section.Id && x.Status != ClassOfferingStatus.ARCHIVED)
                    .OrderBy(x => _subjectService.GetById(x.SubjectId)?.Title ?? string.Empty)
                    .ToList();

                foreach (var offering in offerings)
                {
                    var subject = _subjectService.GetById(offering.SubjectId);
                    var curriculumName = offering.CurriculumId.HasValue
                        ? _curricula.FirstOrDefault(x => x.Id == offering.CurriculumId.Value)?.Name
                        : curriculum?.Name;

                    _enrollmentPlacementOptions.Add(new EnrollmentSubjectOption
                    {
                        SchoolYearId = schoolYear.Id,
                        SchoolYearName = schoolYear.Name,
                        GradeLevelId = section.GradeLevelId,
                        GradeDisplay = grade?.Code ?? grade?.Name ?? "(Unknown Grade)",
                        SectionId = section.Id,
                        SectionName = section.Name,
                        SubjectId = offering.SubjectId,
                        SubjectDisplay = subject?.Title ?? subject?.Code ?? "(Unknown Subject)",
                        Capacity = section.Capacity,
                        EnrolledCount = enrolledCount,
                        AvailableSeats = availableSeats,
                        CapacityDisplay = section.Capacity.HasValue ? section.Capacity.Value.ToString() : "Open",
                        AvailableSeatsDisplay = section.Capacity.HasValue ? availableSeats.ToString() : "Open",
                        CurriculumDisplay = curriculumName ?? "(Select curriculum)",
                        PlacementStatus = offering.Status.ToString()
                    });
                }
            }

            gridEnrollmentClasses.ItemsSource = null;
            gridEnrollmentClasses.ItemsSource = _enrollmentPlacementOptions;
            UpdateEnrollmentRecommendedOfferingText();
        }

        private void LoadEnrollmentStudents(long? preferredStudentId = null)
        {
            var schoolYear = cboEnrollSchoolYear.SelectedItem as SchoolYear;
            var gradeFilter = cboEnrollGrade.SelectedItem as GradeLevel;
            var search = (txtEnrollSearch.Text ?? string.Empty).Trim();
            var enrollments = _enrollmentService.GetAll().ToList();
            _cachedEnrollments = enrollments;
            _enrollmentSnapshotUpdatedAtByStudentId.Clear();
            _enrollmentQueueSeverityByStudentId.Clear();

            var enrollmentByStudent = schoolYear == null
                ? new Dictionary<long, Enrollment>()
                : enrollments
                    .Where(x => x.SchoolYearId == schoolYear.Id)
                    .GroupBy(x => x.StudentId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EnrolledAt).ThenByDescending(x => x.Id).First());

            var requirementsByStudentId = _studentRequirementService.GetAll()
                .GroupBy(x => x.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            _enrollmentTable = new DataTable();
            _enrollmentTable.Columns.Add("Id", typeof(long));
            _enrollmentTable.Columns.Add("Student No");
            _enrollmentTable.Columns.Add("LRN");
            _enrollmentTable.Columns.Add("Student");
            _enrollmentTable.Columns.Add("Preferred Grade");
            _enrollmentTable.Columns.Add("Requirements");
            _enrollmentTable.Columns.Add("Status");

            foreach (var student in _students)
            {
                var enrollment = enrollmentByStudent.TryGetValue(student.Id, out var existing) ? existing : null;
                if (enrollment != null)
                {
                    continue;
                }

                var requirementSnapshot = _requirementChecklistService.BuildForStudent(
                    student.Id,
                    requirementsByStudentId.TryGetValue(student.Id, out var reqs) ? reqs : new List<StudentRequirement>());
                var gradeLabel = _gradeLevels.FirstOrDefault(x => x.Id == student.PreferredGradeLevelId)?.Code
                    ?? _gradeLevels.FirstOrDefault(x => x.Id == student.PreferredGradeLevelId)?.Name
                    ?? "(Not set)";
                var curriculumLabel = _curricula.FirstOrDefault(x => x.Id == student.PreferredCurriculumId)?.Name ?? "(Not set)";
                var readinessText = requirementSnapshot.MissingCount == 0
                    ? "Ready for placement"
                    : $"{requirementSnapshot.MissingCount} requirement(s) pending";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var matches = (student.StudentNumber ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                  (student.Lrn ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                  ($"{student.LastName}, {student.FirstName}").Contains(search, StringComparison.OrdinalIgnoreCase);
                    if (!matches)
                    {
                        continue;
                    }
                }

                if (gradeFilter != null && student.PreferredGradeLevelId != gradeFilter.Id)
                {
                    continue;
                }

                _enrollmentSnapshotUpdatedAtByStudentId[student.Id] = null;
                _enrollmentQueueSeverityByStudentId[student.Id] = EnrollmentQueueSlaSeverity.None;

                _enrollmentTable.Rows.Add(
                    student.Id,
                    student.StudentNumber,
                    student.Lrn,
                    $"{student.LastName}, {student.FirstName}",
                    gradeLabel,
                    requirementSnapshot.MissingCount == 0 ? "Complete" : $"{requirementSnapshot.MissingCount} missing",
                    readinessText);
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
            else if (gridEnrollmentStudents.Items.Count > 0)
            {
                gridEnrollmentStudents.SelectedIndex = 0;
            }
            else
            {
                _selectedEnrollmentStudentId = null;
                UpdateEnrollmentReviewContext(null);
            }

            RefreshSectionSubjectGrid();
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
            _selectedEnrollmentSnapshotUpdatedAtUtc = null;
            _selectedEnrollmentSnapshotStatus = "NOT_ENROLLED";
            _selectedEnrollmentSnapshotApproval = "-";

            var student = _students.FirstOrDefault(x => x.Id == _selectedEnrollmentStudentId.Value);
            if (student?.PreferredGradeLevelId.HasValue == true)
            {
                var grade = _gradeLevels.FirstOrDefault(x => x.Id == student.PreferredGradeLevelId.Value);
                if (grade != null)
                {
                    _suppressEnrollmentEvents = true;
                    cboEnrollGrade.SelectedItem = grade;
                    _suppressEnrollmentEvents = false;
                    SetSessionStateLong("enrollment.gradeId", grade.Id);
                    BindEnrollmentSections();
                }
            }

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
                "Student",
                "Preferred Grade",
                "Requirements",
                "Status"
            };

            if (!allowed.Contains(e.PropertyName))
            {
                e.Cancel = true;
                return;
            }

            if (e.PropertyName == "Student")
            {
                e.Column.Width = new DataGridLength(2.25, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 0;
                return;
            }

            if (e.PropertyName == "Preferred Grade")
            {
                e.Column.Width = new DataGridLength(0.8, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 1;
                return;
            }

            if (e.PropertyName == "Requirements")
            {
                e.Column.Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 2;
                return;
            }

            if (e.PropertyName == "Status")
            {
                e.Column.Width = new DataGridLength(1.35, DataGridLengthUnitType.Star);
                e.Column.DisplayIndex = 3;
            }
        }

        private void GridEnrollmentStudents_LoadingRow(object? sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is not DataRowView row)
            {
                return;
            }

            var readiness = row.Row["Status"]?.ToString() ?? string.Empty;
            if (readiness.Contains("pending", StringComparison.OrdinalIgnoreCase))
            {
                e.Row.Background = new SolidColorBrush(Color.FromRgb(255, 249, 236));
                return;
            }

            e.Row.ClearValue(Control.BackgroundProperty);
        }

        private void GridEnrollmentClasses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridEnrollmentClasses.SelectedItem is not EnrollmentSubjectOption option)
            {
                UpdateEnrollmentRecommendedOfferingText();
                return;
            }

            var grade = _gradeLevels.FirstOrDefault(x => x.Id == option.GradeLevelId);
            var section = _sections.FirstOrDefault(x => x.Id == option.SectionId);

            _suppressEnrollmentEvents = true;
            if (grade != null)
            {
                cboEnrollGrade.SelectedItem = grade;
            }
            cboEnrollSection.SelectedItem = section;
            _suppressEnrollmentEvents = false;

            SetSessionStateLong("enrollment.gradeId", grade?.Id);
            SetSessionStateLong("enrollment.sectionId", section?.Id);
            UpdateEnrollmentRecommendedOfferingText();
        }

        private void SyncPlacementGridSelection()
        {
            if (cboEnrollSection.SelectedItem is not Section section)
            {
                gridEnrollmentClasses.SelectedItem = null;
                return;
            }

            var matches = _enrollmentPlacementOptions.Where(x => x.SectionId == section.Id).Cast<object>().ToList();
            if (matches.Count == 0)
            {
                gridEnrollmentClasses.SelectedItem = null;
                return;
            }

            gridEnrollmentClasses.SelectedIndex = 0;
            gridEnrollmentClasses.ScrollIntoView(gridEnrollmentClasses.Items[0]);
        }

        private void SyncPlacementSelectionFromSection()
        {
            if (cboEnrollSection.SelectedItem is not Section section)
            {
                return;
            }

            var grade = _gradeLevels.FirstOrDefault(x => x.Id == section.GradeLevelId);
            if (grade != null)
            {
                _suppressEnrollmentEvents = true;
                cboEnrollGrade.SelectedItem = grade;
                _suppressEnrollmentEvents = false;
                SetSessionStateLong("enrollment.gradeId", grade.Id);
            }

            SyncPlacementGridSelection();
        }

        private Enrollment? GetSelectedEnrollment()
        {
            if (!_selectedEnrollmentStudentId.HasValue || cboEnrollSchoolYear.SelectedItem is not SchoolYear schoolYear)
            {
                return null;
            }

            return _cachedEnrollments
                .FirstOrDefault(x => x.SchoolYearId == schoolYear.Id && x.StudentId == _selectedEnrollmentStudentId.Value);
        }

        private void UpdateEnrollmentQueueInfo()
        {
            var total = _enrollmentTable?.Rows?.Count ?? 0;
            var ready = 0;
            var pendingRequirements = 0;
            var schoolYear = cboEnrollSchoolYear.SelectedItem as SchoolYear;

            if (_enrollmentTable?.Rows != null)
            {
                foreach (DataRow row in _enrollmentTable.Rows)
                {
                    var readiness = row["Status"]?.ToString() ?? string.Empty;
                    if (readiness.Contains("Ready", StringComparison.OrdinalIgnoreCase))
                    {
                        ready++;
                    }
                    else
                    {
                        pendingRequirements++;
                    }
                }
            }

            var queueSummary =
                $"School Year: {schoolYear?.Name ?? "-"} | Not Enrolled: {total} | Ready: {ready} | Pending Requirements: {pendingRequirements}";
            var settingsSuffix = BuildEnrollmentSettingsSuffix(schoolYear);
            txtEnrollmentQueueInfo.Text = string.IsNullOrWhiteSpace(settingsSuffix)
                ? queueSummary
                : $"{queueSummary} | {settingsSuffix}";
            txtEnrollTopEnrolled.Text = schoolYear == null
                ? "0"
                : _cachedEnrollments.Count(x => x.SchoolYearId == schoolYear.Id && x.Status == EnrollmentStatus.ENROLLED).ToString();
        }

        private string BuildEnrollmentSettingsSuffix(SchoolYear? schoolYear)
        {
            try
            {
                var setting = _schoolSettingService.GetLatest();
                var guidance = _schoolSettingService.GetEnrollmentConfiguration();
                var openDate = schoolYear?.EnrollmentOpenDate?.Date ?? setting?.EnrollmentOpenDate?.Date;
                var closeDate = schoolYear?.EnrollmentCloseDate?.Date ?? setting?.EnrollmentCloseDate?.Date;

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(guidance))
                {
                    parts.Add(guidance);
                }

                if (openDate.HasValue || closeDate.HasValue)
                {
                    var source = schoolYear?.EnrollmentOpenDate.HasValue == true || schoolYear?.EnrollmentCloseDate.HasValue == true
                        ? "school year"
                        : "system settings";
                    if (openDate.HasValue && closeDate.HasValue)
                    {
                        parts.Add($"Window ({source}): {openDate.Value:yyyy-MM-dd} to {closeDate.Value:yyyy-MM-dd}");
                    }
                    else if (openDate.HasValue)
                    {
                        parts.Add($"Opens ({source}): {openDate.Value:yyyy-MM-dd}");
                    }
                    else
                    {
                        parts.Add($"Closes ({source}): {closeDate!.Value:yyyy-MM-dd}");
                    }
                }

                return string.Join(" | ", parts);
            }
            catch
            {
                return string.Empty;
            }
        }

        private void UpdateEnrollmentReviewContext(Enrollment? enrollment)
        {
            var student = _selectedEnrollmentStudentId.HasValue
                ? _students.FirstOrDefault(x => x.Id == _selectedEnrollmentStudentId.Value)
                : null;

            if (student == null)
            {
                txtEnrollReviewStudent.Text = "No student selected.";
                txtEnrollReviewState.Text = "Waiting for selection";
                txtEnrollReviewMeta.Text = string.Empty;
                txtEnrollRequirementSummary.Text = "No student selected.";
                txtEnrollSelectedYear.Text = (cboEnrollSchoolYear.SelectedItem as SchoolYear)?.Name ?? string.Empty;
                txtEnrollRecommendedSection.Text = string.Empty;
                txtEnrollPlacementSummary.Text = "Select a grade offering to review seats and placement outcome.";
                txtEnrollValidationSummary.Text = string.Empty;
                enrollRequirementChecklistPanel.Items = Array.Empty<RequirementChecklistItem>();
                enrollRequirementChecklistPanel.SummaryText = "No student selected.";
                HideEnrollmentStaleWarning();
                return;
            }

            txtEnrollReviewStudent.Text = $"{student.LastName}, {student.FirstName}";
            var requirementSnapshot = UpdateEnrollmentRequirementChecklist(student.Id);
            var preferredGrade = _gradeLevels.FirstOrDefault(x => x.Id == student.PreferredGradeLevelId)?.Code
                ?? _gradeLevels.FirstOrDefault(x => x.Id == student.PreferredGradeLevelId)?.Name
                ?? "(Not set)";
            var preferredCurriculum = _curricula.FirstOrDefault(x => x.Id == student.PreferredCurriculumId)?.Name ?? "(Not set)";
            txtEnrollReviewMeta.Text = preferredGrade;
            txtEnrollRequirementSummary.Text = requirementSnapshot.MissingCount == 0
                ? preferredCurriculum
                : $"{preferredCurriculum} | {requirementSnapshot.MissingCount} missing";
            txtEnrollSelectedYear.Text = (cboEnrollSchoolYear.SelectedItem as SchoolYear)?.Name ?? string.Empty;

            if (enrollment == null)
            {
                txtEnrollReviewState.Text = "Not enrolled";
                UpdateEnrollmentRecommendedOfferingText();
                HideEnrollmentStaleWarning();
                return;
            }

            var waitlistText = enrollment.WaitlistPosition.HasValue ? $" | Waitlist: #{enrollment.WaitlistPosition.Value}" : string.Empty;
            txtEnrollReviewState.Text = $"{enrollment.Status} | {enrollment.ApprovalStatus}{waitlistText}";
            UpdateEnrollmentRecommendedOfferingText();
            HideEnrollmentStaleWarning();
        }

        private void UpdateEnrollmentRecommendedOfferingText()
        {
            if (gridEnrollmentClasses.SelectedItem is not EnrollmentSubjectOption option)
            {
                txtEnrollRecommendedSection.Text = string.Empty;
                txtEnrollPlacementSummary.Text = "Select a section to view the listed subjects and seat availability.";
                return;
            }

            txtEnrollRecommendedSection.Text = $"{option.GradeDisplay} - {option.SectionName}";
            txtEnrollPlacementSummary.Text =
                $"Subject: {option.SubjectDisplay}{Environment.NewLine}" +
                $"School Year: {option.SchoolYearName}{Environment.NewLine}" +
                $"Capacity: {option.CapacityDisplay} | Enrolled: {option.EnrolledCount} | Available: {option.AvailableSeatsDisplay}{Environment.NewLine}" +
                $"Curriculum: {option.CurriculumDisplay}{Environment.NewLine}" +
                $"Status: {option.PlacementStatus}";
        }

        private RequirementChecklistSnapshot UpdateEnrollmentRequirementChecklist(long studentId)
        {
            var requirements = _studentRequirementService.GetAll()
                .Where(x => x.StudentId == studentId)
                .ToList();
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
                MessageBox.Show("Select a student row first.", "Enrollment", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
        }

        private void SelectNextEnrollmentForReview()
        {
            if (gridEnrollmentStudents.Items.Count == 0)
            {
                MessageBox.Show("No not-enrolled students are available.", "Enrollment", MessageBoxButton.OK, MessageBoxImage.Information);
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

                var readiness = row.Row["Status"]?.ToString() ?? string.Empty;
                if (!readiness.Contains("Ready", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                gridEnrollmentStudents.SelectedIndex = index;
                gridEnrollmentStudents.ScrollIntoView(gridEnrollmentStudents.Items[index]);
                return;
            }

            MessageBox.Show("No additional ready-for-placement students were found.", "Enrollment", MessageBoxButton.OK, MessageBoxImage.Information);
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
                ShowEnrollmentStaleWarning("Enrollment record no longer exists. Reload the page before proceeding.");
                TryRaiseEnrollmentConcurrencyException(
                    currentEnrollment,
                    "Enrollment record no longer exists while processing decision.");
                MessageBox.Show("Enrollment record no longer exists. Reload the page.", actionName, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (latest.UpdatedAt != _selectedEnrollmentSnapshotUpdatedAtUtc.Value)
            {
                ShowEnrollmentStaleWarning("Selected enrollment changed after the page was loaded. Reload selected row before continuing.");
                TryRaiseEnrollmentConcurrencyException(
                    currentEnrollment,
                    "Selected enrollment changed after page load.");
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
                    SourceModule = "Enrollment.PlacementPage",
                    Summary = summary,
                    Details = $"EnrollmentId={enrollment.Id}; StudentId={enrollment.StudentId}; SchoolYearId={enrollment.SchoolYearId}",
                    Severity = ExceptionQueueSeverity.WARNING
                });
            }
            catch
            {
                // Swallow exception queue failures to avoid blocking primary action feedback.
            }
        }

        private bool ConfirmWithSummary(EnrollmentDraft draft, long? existingEnrollmentId, string title, out EnrollmentValidationSummary? summary)
        {
            summary = null;
            var summaryResult = _enrollmentService.BuildValidationSummary(draft, existingEnrollmentId);
            if (!summaryResult.Success || summaryResult.Data == null)
            {
                AppFeedbackService.ShowDetailedWarning(summaryResult.Message, summaryResult.Errors, title, this);
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
                MessageBox.Show("Select a student, school year, grade offering, and curriculum first.", "Enrollment", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ConfirmWithSummary(draft, null, "Submit Enrollment Request", out _))
            {
                return;
            }

            var result = _enrollmentService.SubmitEnrollmentRequest(draft);
            if (!result.Success || result.Data == null)
            {
                AppFeedbackService.ShowDetailedWarning(result.Message, result.Errors, "Enrollment", this);
                return;
            }

            AuditTrailService.Log("CREATE", "enrollments", result.Data.Id, null, result.Data);
            AppFeedbackService.ShowSuccess(result.Message, "Enrollment", this);
            var nextStudentId = ResolveNextAvailableStudentId();
            _selectedEnrollmentStudentId = null;
            LoadEnrollmentStudents(nextStudentId);
        }

        private long? ResolveNextAvailableStudentId()
        {
            foreach (var item in gridEnrollmentStudents.Items)
            {
                if (item is not DataRowView row)
                {
                    continue;
                }

                var studentId = row.Row.Field<long>("Id");
                if (_selectedEnrollmentStudentId.HasValue && studentId == _selectedEnrollmentStudentId.Value)
                {
                    continue;
                }

                return studentId;
            }

            return null;
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
                AppFeedbackService.ShowDetailedWarning(result.Message, result.Errors, "Transfer / Update", this);
                return;
            }

            AuditTrailService.Log("UPDATE", "enrollments", result.Data.Id, null, result.Data);
            AppFeedbackService.ShowSuccess(result.Message, "Transfer / Update", this);
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
                AppFeedbackService.ShowDetailedWarning(result.Message, result.Errors, "Approval", this);
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
            AppFeedbackService.ShowSuccess(result.Message, "Approval", this);
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
                AppFeedbackService.ShowDetailedWarning(result.Message, result.Errors, "Return for Correction", this);
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
            AppFeedbackService.ShowSuccess(result.Message, "Return for Correction", this);
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

            if (!AppFeedbackService.Confirm("Cancel this enrollment request?", "Cancel Enrollment", this))
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
                AppFeedbackService.ShowDetailedWarning(result.Message, result.Errors, "Cancel Enrollment", this);
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
            AppFeedbackService.ShowSuccess(result.Message, "Cancel Enrollment", this);
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

            if (!AppFeedbackService.Confirm($"Set enrollment status to {targetStatus}?", "Drop", this))
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
                AppFeedbackService.ShowDetailedWarning(result.Message, result.Errors, "Drop", this);
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
            AppFeedbackService.ShowSuccess(result.Message, "Drop", this);
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
                AppFeedbackService.ShowDetailedWarning(result.Message, result.Errors, "Waitlist", this);
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
            AppFeedbackService.ShowSuccess(result.Message, "Waitlist", this);
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
                AppFeedbackService.ShowDetailedWarning(result.Message, result.Errors, "Status Update", this);
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
            AppFeedbackService.ShowSuccess(result.Message, "Status Update", this);
            LoadEnrollmentStudents(_selectedEnrollmentStudentId);
        }

        private EnrollmentActionReason? PromptEnrollmentActionReason(string title, string message)
        {
            var prompt = new ReasonPromptWindow(
                title,
                message,
                EnrollmentGovernanceReasonOptions)
            {
                Owner = this
            };
            if (prompt.ShowDialog() != true)
            {
                return null;
            }

            return new EnrollmentActionReason
            {
                ReasonCode = prompt.SelectedReasonCode,
                ReasonText = prompt.ReasonDetail
            };
        }
    }
}

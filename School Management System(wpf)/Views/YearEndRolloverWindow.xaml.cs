using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class YearEndRolloverWindow : Window
    {
        private const string RolloverTypedConfirmation = "ROLLOVER";

        private readonly SchoolYearService _schoolYearService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly SectionService _sectionService = new();
        private readonly EnrollmentService _enrollmentService = new();
        private readonly ClassOfferingService _offeringService = new();
        private readonly ClassStudentService _classStudentService = new();
        private readonly ArchiveRecordService _archiveService = new();
        private readonly PermissionBoundaryService _permissionBoundary = new();
        private readonly GovernedOperationLogService _operationLogService = new();
        private readonly ExceptionQueueService _exceptionQueueService = new();
        private readonly PreflightPipelineService _preflightPipeline = new();
        private bool _previewCompleted;
        private bool _preflightPassed;
        private PreflightEvaluationResult? _lastPreflight;

        public YearEndRolloverWindow()
        {
            InitializeComponent();

            btnPreview.Click += (_, _) => Preview();
            btnRun.Click += (_, _) => Execute();
            cboSourceYear.SelectionChanged += (_, _) => OnRolloverInputsChanged();
            cboTargetYear.SelectionChanged += (_, _) => OnRolloverInputsChanged();
            txtRunAcknowledge.TextChanged += (_, _) => EvaluatePreflight();

            chkCreateSnapshot.IsChecked = true;
            chkCloseSourceYear.IsChecked = true;
            btnRun.IsEnabled = false;

            LoadYears();
            txtResult.Text = "Select source/target school year, preview, then run rollover.";
            EvaluatePreflight();
        }

        private void LoadYears()
        {
            var years = _schoolYearService.GetAll()
                .Where(y => !y.IsArchived)
                .OrderByDescending(y => y.Name)
                .ToList();

            cboSourceYear.ItemsSource = years.ToList();
            cboTargetYear.ItemsSource = years.ToList();

            var active = years.FirstOrDefault(y => y.Status == SchoolYearStatus.ACTIVE);
            if (active != null)
            {
                cboSourceYear.SelectedValue = active.Id;
            }

            cboTargetYear.SelectedIndex = years.Count > 1 ? 1 : (years.Count > 0 ? 0 : -1);
        }

        private void Preview()
        {
            txtResult.Text = BuildSummary(runMutation: false);
            _previewCompleted = true;
            EvaluatePreflight();
        }

        private void Execute()
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var correlationId = CorrelationContext.Ensure();

            try
            {
                _permissionBoundary.EnsureAllowed(PolicyActionKey.MAINTENANCE_YEAR_END_ROLLOVER_EXECUTE);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Rollover", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EvaluatePreflight();
            if (!_preflightPassed)
            {
                try
                {
                    _operationLogService.Log(
                        PolicyActionKey.MAINTENANCE_YEAR_END_ROLLOVER_EXECUTE,
                        "YEAR_END_ROLLOVER_BLOCKED",
                        "school_year_rollover",
                        null,
                        GovernedOperationStatus.BLOCKED,
                        "Year-end rollover preflight blocked.",
                        payload: _lastPreflight?.Checks.Select(x => new { x.Code, x.Message, x.Outcome }).ToList(),
                        correlationId: correlationId);

                    _exceptionQueueService.Raise(new ExceptionQueueCreateRequest
                    {
                        Category = "PREFLIGHT_FAILURE",
                        SourceModule = "Operations.YearEndRollover",
                        Entity = "school_year_rollover",
                        Severity = ExceptionQueueSeverity.CRITICAL,
                        Summary = "Year-end rollover preflight blocked.",
                        Details = string.Join(Environment.NewLine, (_lastPreflight?.BlockingReasons ?? new List<string>())),
                        CorrelationId = correlationId
                    });
                }
                catch
                {
                    // Safety logging should not block UI feedback.
                }

                MessageBox.Show("Preflight has not passed. Review checks and complete required confirmation before running rollover.", "Rollover", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Run year-end promotion and enrollment rollover now?",
                "Confirm Rollover",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _operationLogService.Log(
                PolicyActionKey.MAINTENANCE_YEAR_END_ROLLOVER_EXECUTE,
                "YEAR_END_ROLLOVER_START",
                "school_year_rollover",
                null,
                GovernedOperationStatus.STARTED,
                "Year-end rollover execution started.",
                correlationId: correlationId);

            try
            {
                txtResult.Text = BuildSummary(runMutation: true);
                _operationLogService.Log(
                    PolicyActionKey.MAINTENANCE_YEAR_END_ROLLOVER_EXECUTE,
                    "YEAR_END_ROLLOVER_SUCCESS",
                    "school_year_rollover",
                    null,
                    GovernedOperationStatus.SUCCEEDED,
                    "Year-end rollover execution completed.",
                    correlationId: correlationId);
                MessageBox.Show("Year-end rollover completed. Review summary details.", "Rollover", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _operationLogService.Log(
                    PolicyActionKey.MAINTENANCE_YEAR_END_ROLLOVER_EXECUTE,
                    "YEAR_END_ROLLOVER_FAILED",
                    "school_year_rollover",
                    null,
                    GovernedOperationStatus.FAILED,
                    ex.Message,
                    correlationId: correlationId);
                MessageBox.Show($"Year-end rollover failed: {ex.Message}", "Rollover", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string BuildSummary(bool runMutation)
        {
            if (!TryGetSelectedId(cboSourceYear.SelectedValue, out var sourceYearId) || !TryGetSelectedId(cboTargetYear.SelectedValue, out var targetYearId))
            {
                return "Select source and target school years.";
            }

            if (sourceYearId == targetYearId)
            {
                return "Source and target school years must be different.";
            }

            var sourceYear = _schoolYearService.GetById(sourceYearId);
            var targetYear = _schoolYearService.GetById(targetYearId);
            if (sourceYear == null || targetYear == null)
            {
                return "One or both selected school years were not found.";
            }

            var grades = _gradeLevelService.GetAll().ToList();
            var gradeByCode = grades.ToDictionary(g => g.Code, g => g);
            var sections = _sectionService.GetAll().Where(s => s.SchoolYearId == targetYearId && !s.IsArchived).ToList();
            var enrollments = _enrollmentService.GetAll().ToList();

            var sourceEnrollments = enrollments
                .Where(e => e.SchoolYearId == sourceYearId && e.Status == EnrollmentStatus.ENROLLED)
                .ToList();

            var created = 0;
            var skippedExisting = 0;
            var skippedNoGradeMap = 0;
            var skippedNoSection = 0;
            var skippedCapacity = 0;
            var failedCreation = 0;
            var classStudentLinks = 0;
            var details = new List<string>();

            if (runMutation && chkCreateSnapshot.IsChecked == true)
            {
                CreateSnapshot(sourceYearId, sourceYear.Name, enrollments);
            }

            foreach (var enrollment in sourceEnrollments)
            {
                var hasExistingTarget = enrollments.Any(e => e.SchoolYearId == targetYearId && e.StudentId == enrollment.StudentId);
                if (hasExistingTarget)
                {
                    skippedExisting++;
                    continue;
                }

                var sourceGrade = grades.FirstOrDefault(g => g.Id == enrollment.GradeLevelId);
                var nextGrade = ResolveNextGrade(sourceGrade, gradeByCode);
                if (nextGrade == null)
                {
                    skippedNoGradeMap++;
                    details.Add($"Student {enrollment.StudentId}: no next grade mapping from {sourceGrade?.Code ?? "N/A"}.");
                    continue;
                }

                var targetSectionOptions = sections
                    .Where(s => s.GradeLevelId == nextGrade.Id)
                    .OrderBy(s => s.Name)
                    .ToList();

                if (targetSectionOptions.Count == 0)
                {
                    skippedNoSection++;
                    details.Add($"Student {enrollment.StudentId}: no section in {targetYear.Name} for {nextGrade.Code}.");
                    continue;
                }

                Section? chosenSection = null;
                foreach (var option in targetSectionOptions)
                {
                    var currentCount = enrollments.Count(e => e.SchoolYearId == targetYearId && e.SectionId == option.Id && e.Status == EnrollmentStatus.ENROLLED);
                    if (!option.Capacity.HasValue || currentCount < option.Capacity.Value)
                    {
                        chosenSection = option;
                        break;
                    }
                }

                if (chosenSection == null)
                {
                    skippedCapacity++;
                    details.Add($"Student {enrollment.StudentId}: no capacity available in {nextGrade.Code} sections.");
                    continue;
                }

                if (!runMutation)
                {
                    created++;
                    continue;
                }

                var newEnrollment = new Enrollment
                {
                    SchoolYearId = targetYearId,
                    StudentId = enrollment.StudentId,
                    GradeLevelId = nextGrade.Id,
                    SectionId = chosenSection.Id,
                    CurriculumId = enrollment.CurriculumId,
                    Status = EnrollmentStatus.ENROLLED,
                    EnrolledAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                try
                {
                    _enrollmentService.Create(newEnrollment);
                    AuditTrailService.Log("CREATE", "enrollments", newEnrollment.Id, null, newEnrollment);
                    created++;
                }
                catch (Exception ex)
                {
                    failedCreation++;
                    details.Add($"Student {enrollment.StudentId}: rollover enrollment failed ({ex.Message}).");
                    continue;
                }

                var offerings = _offeringService.GetAll()
                    .Where(o => o.SchoolYearId == targetYearId && o.SectionId == chosenSection.Id)
                    .ToList();
                foreach (var offering in offerings)
                {
                    var link = new ClassStudent
                    {
                        ClassOfferingId = offering.Id,
                        StudentId = newEnrollment.StudentId,
                        EnrollmentId = newEnrollment.Id,
                        Status = ClassStudentStatus.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _classStudentService.Create(link);
                    classStudentLinks++;
                }
            }

            if (runMutation && chkCloseSourceYear.IsChecked == true)
            {
                sourceYear.Status = SchoolYearStatus.CLOSED;
                sourceYear.UpdatedAt = DateTime.UtcNow;
                _schoolYearService.Update(sourceYear);
                AuditTrailService.Log("UPDATE", "school_years", sourceYear.Id, null, new { sourceYear.Status });
            }

            var lines = new List<string>
            {
                $"Source: {sourceYear.Name}",
                $"Target: {targetYear.Name}",
                $"Candidates: {sourceEnrollments.Count}",
                $"Created Enrollments: {created}",
                $"Created Class-Student Links: {classStudentLinks}",
                $"Skipped (already enrolled in target): {skippedExisting}",
                $"Skipped (no grade mapping): {skippedNoGradeMap}",
                $"Skipped (no target section): {skippedNoSection}",
                $"Skipped (capacity full): {skippedCapacity}",
                $"Failed (validation/persistence): {failedCreation}",
                runMutation ? "Mode: EXECUTED" : "Mode: PREVIEW"
            };

            if (details.Count > 0)
            {
                lines.Add(string.Empty);
                lines.Add("Details:");
                lines.AddRange(details.Take(80));
                if (details.Count > 80)
                {
                    lines.Add($"... and {details.Count - 80} more");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private static GradeLevel? ResolveNextGrade(GradeLevel? currentGrade, Dictionary<string, GradeLevel> gradeByCode)
        {
            if (currentGrade == null || string.IsNullOrWhiteSpace(currentGrade.Code))
            {
                return null;
            }

            var code = currentGrade.Code.Trim().ToUpperInvariant();
            if (!code.StartsWith("G", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!int.TryParse(code.Substring(1), out var gradeNumber))
            {
                return null;
            }

            var nextCode = $"G{gradeNumber + 1}";
            return gradeByCode.TryGetValue(nextCode, out var next) ? next : null;
        }

        private void CreateSnapshot(long sourceYearId, string sourceYearName, List<Enrollment> allEnrollments)
        {
            var snapshot = new
            {
                SchoolYearId = sourceYearId,
                SchoolYear = sourceYearName,
                // Use the already-loaded enrollment list — avoids a redundant GetAll() call
                Enrollments = allEnrollments.Where(e => e.SchoolYearId == sourceYearId).ToList(),
                Sections = _sectionService.GetAll().Where(s => s.SchoolYearId == sourceYearId).ToList(),
                Offerings = _offeringService.GetAll().Where(o => o.SchoolYearId == sourceYearId).ToList(),
                CapturedAt = DateTime.UtcNow
            };

            _archiveService.Create(new ArchiveRecord
            {
                EntityType = "SchoolYearSnapshot",
                OriginalEntityId = sourceYearId,
                Payload = JsonSerializer.Serialize(snapshot),
                DeletedByUserId = SessionContext.CurrentUser?.Id,
                DeletedAt = DateTime.UtcNow,
                IsRestored = false,
                Notes = "Year-end rollover snapshot"
            });
        }

        private static bool TryGetSelectedId(object? value, out long id)
        {
            if (value is long longValue)
            {
                id = longValue;
                return true;
            }

            if (value is int intValue)
            {
                id = intValue;
                return true;
            }

            id = 0;
            return false;
        }

        /// <summary>
        /// Runs a server-side COUNT for enrolled students in a school year — avoids loading the full enrollment table.
        /// </summary>
        private static int CountEnrolledForYear(long schoolYearId)
        {
            using var db = new Data.AppDbContext();
            return db.Enrollments.Count(x =>
                x.SchoolYearId == schoolYearId &&
                x.Status == EnrollmentStatus.ENROLLED);
        }

        private void OnRolloverInputsChanged()
        {
            _previewCompleted = false;
            EvaluatePreflight();
        }

        private void EvaluatePreflight()
        {
            var hasSource = TryGetSelectedId(cboSourceYear.SelectedValue, out var sourceYearId);
            var hasTarget = TryGetSelectedId(cboTargetYear.SelectedValue, out var targetYearId);
            var sourceYear = hasSource ? _schoolYearService.GetById(sourceYearId) : null;
            var targetYear = hasTarget ? _schoolYearService.GetById(targetYearId) : null;
            var sourceCandidates = hasSource
                ? CountEnrolledForYear(sourceYearId)
                : 0;
            var targetSections = hasTarget
                ? _sectionService.GetAll().Count(x => x.SchoolYearId == targetYearId && !x.IsArchived)
                : 0;
            var typedConfirmOk = string.Equals((txtRunAcknowledge.Text ?? string.Empty).Trim(), RolloverTypedConfirmation, StringComparison.OrdinalIgnoreCase);

            _lastPreflight = _preflightPipeline.Evaluate("Year-End Rollover", new Func<PreflightCheckResult>[]
            {
                () =>
                {
                    if (!hasSource || !hasTarget)
                    {
                        return PreflightCheckResult.Block("YEAR_SELECTION", "Select source and target school years.");
                    }

                    return sourceYearId == targetYearId
                        ? PreflightCheckResult.Block("YEAR_SELECTION", "Source and target school years must be different.")
                        : PreflightCheckResult.Pass("YEAR_SELECTION", "School year selection is valid.");
                },
                () => sourceYear == null || targetYear == null
                    ? PreflightCheckResult.Block("YEAR_RECORDS", "Selected school year records must exist.")
                    : PreflightCheckResult.Pass("YEAR_RECORDS", "School year records found."),
                () => sourceCandidates == 0
                    ? PreflightCheckResult.Block("SOURCE_CANDIDATES", "No enrolled candidates found in source year.")
                    : PreflightCheckResult.Pass("SOURCE_CANDIDATES", $"{sourceCandidates} enrolled candidate(s) found in source year."),
                () => targetSections == 0
                    ? PreflightCheckResult.Block("TARGET_SECTIONS", "No active target sections found.")
                    : PreflightCheckResult.Pass("TARGET_SECTIONS", $"{targetSections} active section(s) found in target year."),
                () => !_previewCompleted
                    ? PreflightCheckResult.Block("PREVIEW_REQUIRED", "Run Preview before execute.")
                    : PreflightCheckResult.Pass("PREVIEW_REQUIRED", "Preview completed."),
                () => !typedConfirmOk
                    ? PreflightCheckResult.Block("TYPED_CONFIRM", $"Type '{RolloverTypedConfirmation}' to enable execute.")
                    : PreflightCheckResult.Pass("TYPED_CONFIRM", "Typed confirmation verified.")
            });

            _preflightPassed = _lastPreflight.Success;
            btnRun.IsEnabled = _preflightPassed;
            txtPreflightStatus.Text = string.Join(
                Environment.NewLine,
                _lastPreflight.Checks.Select(x => $"- [{x.Outcome}] {x.Message}"));
        }
    }
}

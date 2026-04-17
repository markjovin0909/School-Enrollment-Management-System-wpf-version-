using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using School_Management_System.Configuration;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.Views;

namespace School_Management_System
{
    public partial class MainWindow : Window
    {
        private readonly User _currentUser;
        private readonly SessionActivityMessageFilter _activityTracker = new();
        private readonly DispatcherTimer _sessionTimer = new() { Interval = TimeSpan.FromSeconds(30) };
        private bool _logoutLogged;
        private bool _isClosingEventActive;

        private readonly StudentService _studentService = new();
        private readonly UserService _userService = new();
        private readonly SchoolSettingService _schoolSettingService = new();
        private readonly StudentAccountService _studentAccountService = new();
        private readonly StudentRequirementService _studentRequirementService = new();
        private readonly RequirementChecklistService _requirementChecklistService = new();
        private readonly EnrollmentQueueSlaService _enrollmentQueueSlaService = new();
        private readonly TeacherService _teacherService = new();
        private readonly AuthService _authService = new();
        private readonly EnrollmentService _enrollmentService = new();
        private readonly ReportPresetHistoryService _reportPresetHistoryService = new();
        private readonly BackupRestoreService _backupRestoreService = new();
        private readonly ExceptionQueueService _exceptionQueueService = new();
        private readonly OperationalMetricsDashboardService _operationalMetricsService = new();
        private readonly SchoolYearService _schoolYearService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly SectionService _sectionService = new();
        private readonly CurriculumService _curriculumService = new();
        private readonly ClassOfferingService _classOfferingService = new();
        private readonly SubjectService _subjectService = new();
        private readonly List<Button> _navButtons = new();
        private readonly List<Button> _opsSubMenuButtons = new();
        private readonly Dictionary<Button, OperationsSection> _opsButtonSections = new();
        private readonly Dictionary<Button, string> _opsButtonLabels = new();
        private readonly Dictionary<string, HostedOperationsModule> _opsModules = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<OperationsSection, ContentControl> _opsHosts = new();
        private readonly Dictionary<OperationsSection, TextBlock> _opsPlaceholders = new();
        private readonly Dictionary<OperationsSection, TextBlock> _opsWorkspaceInfo = new();

        private DataTable _studentsTable = new();
        private DataTable _teachersTable = new();
        private DataTable _enrollmentTable = new();
        private DataTable _reportsTable = new();

        private long? _selectedStudentId;
        private long? _selectedTeacherId;
        private long? _selectedEnrollmentStudentId;

        private bool _suppressStudentEvents;
        private bool _suppressTeacherEvents;
        private bool _suppressEnrollmentEvents;
        private bool _suppressReportEvents;

        private List<SchoolYear> _schoolYears = new();
        private List<GradeLevel> _gradeLevels = new();
        private List<Section> _sections = new();
        private List<Curriculum> _curricula = new();
        private List<Student> _students = new();
        private List<Teacher> _teachers = new();
        private readonly Dictionary<long, EnrollmentQueueSlaSeverity> _enrollmentQueueSeverityByStudentId = new();
        private EnrollmentQueueSlaPolicy _enrollmentQueueSlaPolicy = EnrollmentQueueSlaPolicy.Default;

        private enum OperationsSection
        {
            MasterData,
            Scheduling,
            AccountsCompliance,
            Maintenance
        }

        private sealed class HostedOperationsModule
        {
            public HostedOperationsModule(Window window, UIElement content, OperationsSection section)
            {
                Window = window;
                Content = content;
                Section = section;
            }

            public Window Window { get; }
            public UIElement Content { get; }
            public OperationsSection Section { get; }
        }

        public MainWindow(User currentUser)
        {
            _currentUser = currentUser;
            SessionContext.CurrentUser = _currentUser;
            StructuralSchemaService.EnsureApplied();

            InitializeComponent();

            txtCurrentUser.Text = $"User: {_currentUser.Username}";
            txtCurrentEnvironment.Text = $"Environment: {DatabaseConfig.ActiveEnvironment}";

            WireNavigation();
            WireTopBar();
            InitializeOperationsInlineHosts();
            WireOperationsButtons();
            StartSessionMonitoring();

            InitializeStudentsTab();
            InitializeTeachersTab();
            InitializeEnrollmentTab();
            InitializeReportsTab();

            LoadDashboard();
        }

        private void WireNavigation()
        {
            _navButtons.Clear();
            _navButtons.AddRange(new[]
            {
                btnNavDashboard,
                btnNavStudents,
                btnNavTeachers,
                btnNavEnrollment,
                btnNavReports,
                btnNavMasterData,
                btnNavScheduling,
                btnNavAccountsCompliance,
                btnNavMaintenance
            });

            btnNavDashboard.Click += (_, _) => NavigateMainTab(0);
            btnNavStudents.Click += (_, _) => NavigateMainTab(1);
            btnNavTeachers.Click += (_, _) => NavigateMainTab(2);
            btnNavEnrollment.Click += (_, _) => NavigateMainTab(3);
            btnNavReports.Click += (_, _) => NavigateMainTab(4);
            btnNavMasterData.Click += (_, _) => NavigateMainTab(5);
            btnNavScheduling.Click += (_, _) => NavigateMainTab(6);
            btnNavAccountsCompliance.Click += (_, _) => NavigateMainTab(7);
            btnNavMaintenance.Click += (_, _) => NavigateMainTab(8);

            tabsMain.SelectionChanged += (_, _) => UpdateNavigationState();
            NavigateMainTab(0);
        }

        private void NavigateMainTab(int index)
        {
            tabsMain.SelectedIndex = index;
            UpdateNavigationState();
        }

        private void UpdateNavigationState()
        {
            if (_navButtons.Count == 0)
            {
                return;
            }

            var activeBackground = (Brush)FindResource("Brush.NavButtonActive");
            var inactiveBackground = (Brush)FindResource("Brush.NavButton");
            var activeBorder = (Brush)FindResource("Brush.NavButtonActiveBorder");
            var inactiveBorder = (Brush)FindResource("Brush.NavButtonBorder");
            var activeForeground = (Brush)FindResource("Brush.NavButtonTextActive");
            var inactiveForeground = (Brush)FindResource("Brush.NavButtonText");

            for (var i = 0; i < _navButtons.Count; i++)
            {
                var button = _navButtons[i];
                var active = tabsMain.SelectedIndex == i;
                button.Background = active ? activeBackground : inactiveBackground;
                button.BorderBrush = active ? activeBorder : inactiveBorder;
                button.Foreground = active ? activeForeground : inactiveForeground;
                button.FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold;
                button.Opacity = 1;
            }
        }

        private void WireTopBar()
        {
            btnTopDbSettings.Click += (_, _) => new DatabaseConfigurationWindow { Owner = this }.ShowDialog();
            btnTopChangePassword.Click += (_, _) => new ChangePasswordWindow(_currentUser.Id) { Owner = this }.ShowDialog();
            btnTopActivity.Click += (_, _) => new UserActivityHistoryWindow(_currentUser) { Owner = this }.ShowDialog();
            btnTopLogout.Click += (_, _) => Logout("USER_LOGOUT");

            btnDashboardRefresh.Click += (_, _) => LoadDashboard();

            Closing += (_, _) =>
            {
                _isClosingEventActive = true;
                _activityTracker.Detach();
                _sessionTimer.Stop();
                if (!_logoutLogged)
                {
                    Logout("WINDOW_CLOSED", reopenLogin: false, closeWindow: false);
                }
            };
        }

        private void StartSessionMonitoring()
        {
            _activityTracker.Attach();
            _sessionTimer.Tick += (_, _) =>
            {
                if (!SessionContext.IsExpired())
                {
                    return;
                }

                MessageBox.Show(
                    "Session expired due to inactivity. Please sign in again.",
                    "Session Timeout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Logout("SESSION_TIMEOUT");
            };
            _sessionTimer.Start();
        }

        private void Logout(string reason, bool reopenLogin = true, bool closeWindow = true)
        {
            if (_logoutLogged)
            {
                return;
            }

            _logoutLogged = true;
            _sessionTimer.Stop();
            try
            {
                new AccountSecurityService().LogLogout(_currentUser, reason);
            }
            catch
            {
                // logout should proceed even if audit write fails.
            }

            SessionContext.Clear();
            _activityTracker.Detach();

            if (reopenLogin)
            {
                var login = new LoginWindow();
                Application.Current.MainWindow = login;
                login.Show();
            }

            if (closeWindow && !_isClosingEventActive)
            {
                Close();
            }
        }

        private void LoadDashboard()
        {
            try
            {
                _students = _studentService.GetAll().ToList();
                _teachers = _teacherService.GetAll().ToList();
                _sections = _sectionService.GetAll().ToList();
                var offerings = _classOfferingService.GetAll().ToList();
                var enrollments = _enrollmentService.GetAll().ToList();

                cardStudents.Text = _students.Count.ToString();
                cardTeachers.Text = _teachers.Count.ToString();
                cardSections.Text = _sections.Count(x => !x.IsArchived).ToString();
                cardOfferings.Text = offerings.Count(x => x.Status != ClassOfferingStatus.ARCHIVED).ToString();
                cardEnrolled.Text = enrollments.Count(x => x.Status == EnrollmentStatus.ENROLLED).ToString();
                cardPending.Text = enrollments.Count(x => x.Status == EnrollmentStatus.PENDING).ToString();
                PopulateDashboardWarnings(enrollments);
                PopulateEnrollmentStatusGraph(enrollments);
                PopulateOperationalMetrics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dashboard load failed: {ex.Message}", "Dashboard", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateEnrollmentStatusGraph(List<Enrollment> enrollments)
        {
            var enrolledCount = enrollments.Count(x => x.Status == EnrollmentStatus.ENROLLED);
            var pendingCount = enrollments.Count(x => x.Status == EnrollmentStatus.PENDING);
            var reservedCount = enrollments.Count(x => x.Status == EnrollmentStatus.RESERVED);
            var total = Math.Max(1, enrolledCount + pendingCount + reservedCount);

            txtDashboardEnrollmentTotal.Text = total.ToString();

            var segments = new List<DashboardStatusChartItem>
            {
                new()
                {
                    Label = "Enrolled",
                    Count = enrolledCount,
                    Percentage = enrolledCount * 100d / total,
                    Fill = (Brush)FindResource("Brush.Success")
                },
                new()
                {
                    Label = "Pending",
                    Count = pendingCount,
                    Percentage = pendingCount * 100d / total,
                    Fill = (Brush)FindResource("Brush.Warning")
                },
                new()
                {
                    Label = "Reserved",
                    Count = reservedCount,
                    Percentage = reservedCount * 100d / total,
                    Fill = (Brush)FindResource("Brush.Info")
                }
            };

            foreach (var segment in segments)
            {
                segment.PercentageLabel = $"{segment.Percentage:0.#}%";
                segment.Detail = $"{segment.Count} record(s)";
            }

            gridDashboardEnrollmentSegments.ColumnDefinitions.Clear();
            gridDashboardEnrollmentSegments.Children.Clear();

            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                gridDashboardEnrollmentSegments.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(Math.Max(segment.Count, 1), GridUnitType.Star)
                });

                var bar = new Border
                {
                    Background = segment.Fill
                };

                Grid.SetColumn(bar, i);
                gridDashboardEnrollmentSegments.Children.Add(bar);
            }

            itemsDashboardEnrollmentLegend.ItemsSource = segments;
        }

        private void PopulateOperationalMetrics()
        {
            var snapshot = _operationalMetricsService.BuildSnapshot();
            SetMetric(txtMetricQueueAgingValue, txtMetricQueueAgingTrend, snapshot.QueueAging);
            SetMetric(txtMetricDecisionReversalsValue, txtMetricDecisionReversalsTrend, snapshot.DecisionReversals);
            SetMetric(txtMetricWaitlistPressureValue, txtMetricWaitlistPressureTrend, snapshot.WaitlistPressure);
            SetMetric(txtMetricFailedOpsValue, txtMetricFailedOpsTrend, snapshot.FailedCriticalOps);

            var metrics = snapshot.All
                .Select(metric => new DashboardMetricChartItem
                {
                    Label = metric.Title.Replace(" (7d)", string.Empty),
                    ValueLabel = metric.Value,
                    Trend = metric.Trend,
                    NumericValue = double.TryParse(metric.Value, out var value) ? value : 0,
                    Fill = metric.Severity switch
                    {
                        "critical" => (Brush)FindResource("Brush.Danger"),
                        "warning" => (Brush)FindResource("Brush.Warning"),
                        _ => (Brush)FindResource("Brush.Success")
                    }
                })
                .ToList();

            var maxValue = Math.Max(1d, metrics.Max(x => x.NumericValue));
            foreach (var metric in metrics)
            {
                metric.ColumnHeight = 24d + (metric.NumericValue / maxValue) * 126d;
            }

            itemsDashboardMetricBars.ItemsSource = metrics;
        }

        private void SetMetric(TextBlock valueBlock, TextBlock trendBlock, OperationalTrendMetric metric)
        {
            valueBlock.Text = metric.Value;
            trendBlock.Text = metric.Trend;

            var severityBrush = metric.Severity switch
            {
                "critical" => (Brush)FindResource("Brush.Danger"),
                "warning" => (Brush)FindResource("Brush.Warning"),
                _ => (Brush)FindResource("Brush.Success")
            };

            valueBlock.Foreground = severityBrush;
            trendBlock.Foreground = severityBrush;
        }

        private void PopulateDashboardWarnings(List<Enrollment> enrollments)
        {
            var requirements = _studentRequirementService.GetAll().ToList();
            var requirementGroups = requirements
                .GroupBy(x => x.StudentId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var activeStudents = _students.Where(x => x.Status == UserStatus.ACTIVE).ToList();
            var incompleteRequirementCount = activeStudents.Count(student =>
                !requirementGroups.TryGetValue(student.Id, out var studentRequirements) ||
                studentRequirements.Count == 0 ||
                studentRequirements.Any(r => !r.IsSubmitted));

            txtDashboardRequirementsWarning.Text = incompleteRequirementCount == 0
                ? "No requirement blockers detected for active students."
                : $"{incompleteRequirementCount} active student(s) have incomplete requirements.";

            var waitlisted = enrollments
                .Where(x => x.Status == EnrollmentStatus.RESERVED)
                .ToList();
            if (waitlisted.Count == 0)
            {
                txtDashboardWaitlistWarning.Text = "No waitlisted enrollments. Section capacity is healthy.";
            }
            else
            {
                var topSection = waitlisted
                    .GroupBy(x => x.SectionId)
                    .Select(g => new { SectionId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.SectionId)
                    .First();
                var sectionName = _sections.FirstOrDefault(x => x.Id == topSection.SectionId)?.Name ?? $"Section {topSection.SectionId}";
                txtDashboardWaitlistWarning.Text = $"{waitlisted.Count} waitlisted enrollment(s). Highest pressure: {sectionName} ({topSection.Count}).";
            }

            var history = _backupRestoreService
                .LoadHistory(30)
                .OrderByDescending(x => x.TimestampUtc)
                .ToList();
            var latestFailure = history.FirstOrDefault(x => !string.Equals(x.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase));

            if (latestFailure != null)
            {
                txtDashboardBackupWarning.Text = $"Last failure: {latestFailure.Action} on {latestFailure.TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm}.";
                return;
            }

            var latestRun = history.FirstOrDefault();
            txtDashboardBackupWarning.Text = latestRun == null
                ? "No backup/restore history yet."
                : $"Latest run OK: {latestRun.Action} on {latestRun.TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm}.";
        }

        private sealed class DashboardStatusChartItem
        {
            public string Label { get; set; } = string.Empty;
            public int Count { get; set; }
            public double Percentage { get; set; }
            public string PercentageLabel { get; set; } = string.Empty;
            public string Detail { get; set; } = string.Empty;
            public Brush Fill { get; set; } = Brushes.Transparent;
        }

        private sealed class DashboardMetricChartItem
        {
            public string Label { get; set; } = string.Empty;
            public string ValueLabel { get; set; } = "0";
            public string Trend { get; set; } = string.Empty;
            public double NumericValue { get; set; }
            public double ColumnHeight { get; set; }
            public Brush Fill { get; set; } = Brushes.Transparent;
        }

        private void WireOperationsButtons()
        {
            BindOperationsModuleButton(btnOpsSchoolSettings, OperationsSection.MasterData, "school_settings", () => new SchoolSettingsWindow());
            BindOperationsModuleButton(btnOpsSchoolYears, OperationsSection.MasterData, "school_years", () => new SchoolYearsWindow());
            BindOperationsModuleButton(btnOpsGradeLevels, OperationsSection.MasterData, "grade_levels", () => new GradeLevelsWindow());
            BindOperationsModuleButton(btnOpsSubjects, OperationsSection.MasterData, "subjects", () => new SubjectsWindow());
            BindOperationsModuleButton(btnOpsCurriculum, OperationsSection.MasterData, "curriculum", () => new CurriculumWindow());
            BindOperationsModuleButton(btnOpsSections, OperationsSection.MasterData, "sections", () => new SectionsWindow());

            BindOperationsModuleButton(btnOpsOfferings, OperationsSection.Scheduling, "class_offerings", () => new ClassOfferingsWindow());
            BindOperationsModuleButton(btnOpsSchedules, OperationsSection.Scheduling, "schedules", () => new SchedulesWindow());
            BindOperationsModuleButton(btnOpsTeacherLoads, OperationsSection.Scheduling, "teacher_loads", () => new TeacherLoadsWindow(hostedInline: true));
            BindOperationsModuleButton(btnOpsRooms, OperationsSection.Scheduling, "rooms", () => new RoomsWindow());
            BindOperationsModuleButton(btnOpsTimeSlots, OperationsSection.Scheduling, "time_slots", () => new TimeSlotsWindow());

            BindOperationsModuleButton(btnOpsStudentAccounts, OperationsSection.AccountsCompliance, "student_accounts", () => new StudentAccountsWindow(_currentUser, dialogOwner: this));
            BindOperationsModuleButton(btnOpsRequirements, OperationsSection.AccountsCompliance, "student_requirements", () => new StudentRequirementsWindow());
            BindOperationsModuleButton(btnOpsArchive, OperationsSection.AccountsCompliance, "archive_center", () => new ArchiveCenterWindow());

            BindOperationsModuleButton(btnOpsBackup, OperationsSection.Maintenance, "backup_restore", () => new BackupRestoreWindow(() => Logout("RESTORE_RELOGIN")));
            BindOperationsModuleButton(btnOpsYearEnd, OperationsSection.Maintenance, "year_end_rollover", () => new YearEndRolloverWindow());
        }

        private void InitializeOperationsInlineHosts()
        {
            _opsHosts.Clear();
            _opsPlaceholders.Clear();
            _opsWorkspaceInfo.Clear();

            _opsHosts[OperationsSection.MasterData] = hostMasterDataModule;
            _opsHosts[OperationsSection.Scheduling] = hostSchedulingModule;
            _opsHosts[OperationsSection.AccountsCompliance] = hostAccountsComplianceModule;
            _opsHosts[OperationsSection.Maintenance] = hostMaintenanceModule;

            _opsPlaceholders[OperationsSection.MasterData] = txtMasterDataModulePlaceholder;
            _opsPlaceholders[OperationsSection.Scheduling] = txtSchedulingModulePlaceholder;
            _opsPlaceholders[OperationsSection.AccountsCompliance] = txtAccountsComplianceModulePlaceholder;
            _opsPlaceholders[OperationsSection.Maintenance] = txtMaintenanceModulePlaceholder;

            _opsWorkspaceInfo[OperationsSection.MasterData] = txtMasterDataWorkspaceInfo;
            _opsWorkspaceInfo[OperationsSection.Scheduling] = txtSchedulingWorkspaceInfo;
            _opsWorkspaceInfo[OperationsSection.AccountsCompliance] = txtAccountsComplianceWorkspaceInfo;
            _opsWorkspaceInfo[OperationsSection.Maintenance] = txtMaintenanceWorkspaceInfo;

            foreach (var section in _opsWorkspaceInfo.Keys)
            {
                SetOperationsWorkspaceInfo(section, null);
            }
        }

        private void BindOperationsModuleButton(Button button, OperationsSection section, string moduleKey, Func<Window> moduleFactory)
        {
            _opsSubMenuButtons.Add(button);
            _opsButtonSections[button] = section;
            _opsButtonLabels[button] = button.Content?.ToString() ?? moduleKey;
            button.Click += (_, _) => LoadOperationsModule(section, button, moduleKey, moduleFactory);
        }

        private void LoadOperationsModule(OperationsSection section, Button sourceButton, string moduleKey, Func<Window> moduleFactory)
        {
            if (!_opsHosts.TryGetValue(section, out var host))
            {
                return;
            }

            if (!_opsModules.TryGetValue(moduleKey, out var module))
            {
                module = CreateHostedOperationsModule(section, moduleFactory);
                if (module == null)
                {
                    return;
                }

                _opsModules[moduleKey] = module;
            }

            if (module.Content is FrameworkElement root)
            {
                root.Margin = new Thickness(0);
            }

            host.Content = module.Content;
            host.Visibility = Visibility.Visible;

            if (_opsPlaceholders.TryGetValue(section, out var placeholder))
            {
                placeholder.Visibility = Visibility.Collapsed;
            }

            SetOperationsWorkspaceInfo(section, sourceButton);
            SetActiveOperationsButton(sourceButton, section);
        }

        private HostedOperationsModule? CreateHostedOperationsModule(OperationsSection section, Func<Window> moduleFactory)
        {
            try
            {
                var moduleWindow = moduleFactory();
                if (moduleWindow == null)
                {
                    return null;
                }

                if (moduleWindow.Content is not UIElement content)
                {
                    MessageBox.Show("Unable to load module content.", "Operations", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                moduleWindow.Content = null;
                return new HostedOperationsModule(moduleWindow, content, section);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Module load failed: {ex.Message}", "Operations", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private void SetActiveOperationsButton(Button activeButton, OperationsSection section)
        {
            var selectedBackground = (Brush)FindResource("Brush.SurfaceAlt");
            var selectedBorder = (Brush)FindResource("Brush.Primary");
            var selectedForeground = (Brush)FindResource("Brush.Primary");

            var defaultBackground = (Brush)FindResource("Brush.Surface");
            var defaultBorder = (Brush)FindResource("Brush.BorderStrong");
            var defaultForeground = (Brush)FindResource("Brush.TextPrimary");

            foreach (var button in _opsSubMenuButtons)
            {
                var isActive = ReferenceEquals(button, activeButton);
                var isInActiveSection = _opsButtonSections.TryGetValue(button, out var mappedSection) && mappedSection == section;

                button.Background = isActive ? selectedBackground : defaultBackground;
                button.BorderBrush = isActive ? selectedBorder : defaultBorder;
                button.Foreground = isActive ? selectedForeground : defaultForeground;
                button.FontWeight = isActive ? FontWeights.Bold : FontWeights.SemiBold;
                button.Opacity = isInActiveSection ? 1 : 0.8;
            }
        }

        private void SetOperationsWorkspaceInfo(OperationsSection section, Button? selectedButton)
        {
            if (!_opsWorkspaceInfo.TryGetValue(section, out var info))
            {
                return;
            }

            var sectionLabel = ResolveOperationsSectionLabel(section);
            var moduleCount = _opsButtonSections.Count(x => x.Value == section);
            if (selectedButton == null)
            {
                info.Text = $"{sectionLabel}: {moduleCount} module(s) available. Select one from the left list to open its detail workspace.";
                return;
            }

            var label = _opsButtonLabels.TryGetValue(selectedButton, out var mapped)
                ? mapped
                : selectedButton.Content?.ToString() ?? "Module";
            info.Text = $"{sectionLabel} active module: {label}. Use the left list to switch context without leaving the page.";
        }

        private static string ResolveOperationsSectionLabel(OperationsSection section)
        {
            return section switch
            {
                OperationsSection.MasterData => "Master Data",
                OperationsSection.Scheduling => "Scheduling",
                OperationsSection.AccountsCompliance => "Accounts & Compliance",
                OperationsSection.Maintenance => "Maintenance",
                _ => "Operations"
            };
        }

        private static int? ComputeAge(DateTime? birthdate)
        {
            if (!birthdate.HasValue)
            {
                return null;
            }

            var today = DateTime.Today;
            var age = today.Year - birthdate.Value.Year;
            if (birthdate.Value.Date > today.AddYears(-age))
            {
                age--;
            }

            return age < 0 ? null : age;
        }

        private static EnrollmentDraft? BuildEnrollmentDraft(long? studentId, object schoolYearValue, object sectionValue, object curriculumValue)
        {
            if (!studentId.HasValue || schoolYearValue is not long schoolYearId || sectionValue is not long sectionId || curriculumValue is not long curriculumId)
            {
                return null;
            }

            return new EnrollmentDraft
            {
                SchoolYearId = schoolYearId,
                StudentId = studentId.Value,
                SectionId = sectionId,
                CurriculumId = curriculumId
            };
        }

        private static string BuildDetailedError(string message, IEnumerable<string> errors)
        {
            var details = errors?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
            if (details.Count == 0)
            {
                return message;
            }

            return $"{message}{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, details.Select(x => $"- {x}"))}";
        }
    }
}

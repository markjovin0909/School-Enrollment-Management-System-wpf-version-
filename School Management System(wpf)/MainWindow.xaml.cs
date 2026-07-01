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
        private readonly SchoolBrandingService _schoolBrandingService = new();
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
        private readonly OperationalMetricsDashboardService _operationalMetricsDashboardService = new();
        private readonly ArchiveRecordService _archiveRecordService = new();
        private readonly AuditLogService _auditLogService = new();
        private readonly SchoolYearService _schoolYearService = new();
        private readonly GradingPeriodService _gradingPeriodService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly SectionService _sectionService = new();
        private readonly CurriculumService _curriculumService = new();
        private readonly ClassOfferingService _classOfferingService = new();
        private readonly ClassScheduleService _classScheduleService = new();
        private readonly SubjectService _subjectService = new();
        private readonly List<Button> _opsSubMenuButtons = new();
        private readonly Dictionary<Button, OperationsSection> _opsButtonSections = new();
        private readonly Dictionary<Button, string> _opsButtonLabels = new();
        private readonly Dictionary<string, HostedOperationsModule> _opsModules = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<OperationsSection, ContentControl> _opsHosts = new();
        private readonly Dictionary<OperationsSection, TextBlock> _opsPlaceholders = new();
        private readonly Dictionary<OperationsSection, TextBlock> _opsWorkspaceInfo = new();
        private readonly Dictionary<OperationsSection, Action> _opsDefaultLoaders = new();

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
        private List<Enrollment> _cachedEnrollments = new();
        private readonly Dictionary<long, EnrollmentQueueSlaSeverity> _enrollmentQueueSeverityByStudentId = new();
        private EnrollmentQueueSlaPolicy _enrollmentQueueSlaPolicy = EnrollmentQueueSlaPolicy.Default;
        private readonly List<EnrollmentSubjectOption> _enrollmentPlacementOptions = new();

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

            ApplyBranding();
            txtCurrentUser.Text = $"User: {_currentUser.Username}";
            txtCurrentEnvironment.Text = $"Environment: {DatabaseConfig.ActiveEnvironment}";
            txtTodayInfo.Text = DateTime.Today.ToString("dddd, MMMM dd, yyyy");

            WireNavigation();
            WireTopBar();
            InitializeOperationsInlineHosts();
            WireOperationsButtons();
            StartSessionMonitoring();

            InitializeStudentsTab();
            InitializeTeachersTab();
            InitializeEnrollmentTab();
            InitializeReportsTab();
            InitializeStudentDetailsTab();
            InitializeStudentGradesTab();
            InitializeTeacherDetailsTab();
            InitializeEnrollmentDetailsTab();

            LoadDashboard();
        }

        private void ApplyBranding()
        {
            var branding = _schoolBrandingService.GetCurrentBranding();
            Title = branding.SchoolName;
            txtTopBarSchoolName.Text = branding.SchoolName;
            txtDashboardSchoolName.Text = branding.SchoolName;
            imgTopBarLogo.Source = branding.LogoImage;
            imgDashboardHeaderLogo.Source = branding.LogoImage;
        }

        private void WireNavigation()
        {
            btnTopDashboard.Click += (_, _) => NavigateMainTab(0);
            btnHubStudents.Click += (_, _) => OpenStudentSearchModal();
            btnHubTeachers.Click += (_, _) => OpenTeacherSearchModal();
            btnHubEnrollment.Click += (_, _) => NavigateMainTab(3);
            btnHubReports.Click += (_, _) => NavigateMainTab(4);
            btnHubMasterData.Click += (_, _) => NavigateMainTab(5);
            btnHubScheduling.Click += (_, _) => NavigateMainTab(6);
            btnHubMaintenance.Click += (_, _) => NavigateMainTab(8);
            btnDashActiveStudents.Click += (_, _) => OpenStudentGradesWindow();
            btnDashArchivedRecords.Click += (_, _) => OpenArchiveCenter();

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
            var isDashboard = tabsMain.SelectedIndex == 0;

            btnTopDashboard.Visibility = isDashboard ? Visibility.Collapsed : Visibility.Visible;
            topCommandBar.Visibility = isDashboard ? Visibility.Collapsed : Visibility.Visible;
            mainContentHost.Padding = isDashboard ? new Thickness(0) : new Thickness(14);
            mainContentHost.Background = isDashboard ? Brushes.Transparent : new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xF9));
            rootShell.Background = isDashboard ? new SolidColorBrush(Color.FromRgb(0x0A, 0x3F, 0x3D)) : new SolidColorBrush(Color.FromRgb(0xEE, 0xF3, 0xF9));
        }

        private void WireTopBar()
        {
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
                var enrollments = _enrollmentService.GetAll().ToList();
                var archives = _archiveRecordService.GetAll().ToList();
                var userLookup = _userService.GetAll().ToDictionary(x => x.Id, x => x.Username);
                var metrics = _operationalMetricsDashboardService.BuildSnapshot();
                var activeSchoolYear = _schoolYearService.GetActiveSchoolYear();
                var currentGradingPeriod = GetCurrentDashboardGradingPeriod(activeSchoolYear?.Id);

                var activeStudents = _students.Count(x => x.Status == UserStatus.ACTIVE);
                var enrolledStudents = enrollments.Count(x => x.Status == EnrollmentStatus.ENROLLED);
                var pendingEnrollments = enrollments.Count(x =>
                    x.Status == EnrollmentStatus.PENDING ||
                    (x.ApprovalStatus == EnrollmentApprovalStatus.PENDING && x.Status != EnrollmentStatus.ENROLLED));
                var activeTeachers = _teachers.Count(x => x.Status == UserStatus.ACTIVE);
                var activeEnrollmentsBySection = enrollments
                    .Where(x => x.Status == EnrollmentStatus.ENROLLED)
                    .GroupBy(x => x.SectionId)
                    .ToDictionary(x => x.Key, x => x.Count());
                var availableSections = _sections.Count(section =>
                {
                    if (section.IsArchived)
                    {
                        return false;
                    }

                    if (!section.Capacity.HasValue || section.Capacity.Value <= 0)
                    {
                        return false;
                    }

                    var occupied = activeEnrollmentsBySection.TryGetValue(section.Id, out var count) ? count : 0;
                    return occupied < section.Capacity.Value;
                });
                var archivedRecords = archives.Count(x => !x.IsRestored);

                cardTotalStudents.Value = activeStudents.ToString();
                cardTotalStudents.Hint = $"{_students.Count} total student record(s)";
                cardEnrolledStudents.Value = enrolledStudents.ToString();
                cardEnrolledStudents.Hint = "Enrollment status ENROLLED";
                cardPendingEnrollments.Value = pendingEnrollments.ToString();
                cardPendingEnrollments.Hint = "Pending status or approval";
                cardTotalTeachers.Value = activeTeachers.ToString();
                cardTotalTeachers.Hint = $"{_teachers.Count} total teacher record(s)";
                cardAvailableSections.Value = availableSections.ToString();
                cardAvailableSections.Hint = "Non-archived sections with open seats";
                cardArchivedRecords.Value = archivedRecords.ToString();
                cardArchivedRecords.Hint = "Archived items not yet restored";

                txtDashboardQueueMetric.Text = $"{metrics.QueueAging.Title}: {metrics.QueueAging.Value}  |  {metrics.QueueAging.Trend}";
                txtDashboardReversalMetric.Text = $"{metrics.DecisionReversals.Title}: {metrics.DecisionReversals.Value}  |  {metrics.DecisionReversals.Trend}";
                txtDashboardWaitlistMetric.Text = $"{metrics.WaitlistPressure.Title}: {metrics.WaitlistPressure.Value}  |  {metrics.WaitlistPressure.Trend}";
                txtDashboardCriticalOpsMetric.Text = $"{metrics.FailedCriticalOps.Title}: {metrics.FailedCriticalOps.Value}  |  {metrics.FailedCriticalOps.Trend}";
                txtDashboardSchoolYear.Text = activeSchoolYear != null
                    ? $"School year: {BuildSchoolYearDashboardLabel(activeSchoolYear)}"
                    : "School year: No active school year";
                txtDashboardCurrentGrading.Text = currentGradingPeriod != null
                    ? $"Current grading: {BuildGradingPeriodDashboardLabel(currentGradingPeriod)}"
                    : "Current grading: No grading period configured";

                var activityTable = new DataTable();
                activityTable.Columns.Add("Date");
                activityTable.Columns.Add("User");
                activityTable.Columns.Add("Action");
                activityTable.Columns.Add("Module");

                foreach (var log in _auditLogService.GetAll().OrderByDescending(x => x.CreatedAt).Take(12))
                {
                    activityTable.Rows.Add(
                        log.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        userLookup.TryGetValue(log.UserId, out var username) ? username : $"User {log.UserId}",
                        (log.Action ?? string.Empty).Replace('_', ' '),
                        log.Entity ?? string.Empty);
                }

                gridDashboardActivity.ItemsSource = activityTable.DefaultView;
                txtDashboardUpdatedAt.Text = $"Updated at {DateTime.Now:HH:mm:ss} • {activityTable.Rows.Count} recent activity item(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dashboard load failed: {ex.Message}", "Dashboard", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private GradingPeriod? GetCurrentDashboardGradingPeriod(long? schoolYearId)
        {
            if (!schoolYearId.HasValue)
            {
                return null;
            }

            var today = DateTime.Today;
            var gradingPeriods = _gradingPeriodService.GetAll()
                .Where(x => x.SchoolYearId == schoolYearId.Value)
                .OrderBy(x => x.StartDate ?? DateTime.MaxValue)
                .ThenBy(x => x.EndDate ?? DateTime.MaxValue)
                .ThenBy(x => x.Id)
                .ToList();

            return gradingPeriods
                .Where(x => x.Status == GradingPeriodStatus.OPEN)
                .OrderBy(x => x.StartDate ?? DateTime.MinValue)
                .LastOrDefault()
                ?? gradingPeriods
                    .Where(x => x.StartDate.HasValue && x.EndDate.HasValue && x.StartDate.Value.Date <= today && x.EndDate.Value.Date >= today)
                    .OrderBy(x => x.StartDate ?? DateTime.MinValue)
                    .LastOrDefault()
                ?? gradingPeriods
                    .Where(x => x.Status == GradingPeriodStatus.UPCOMING)
                    .OrderBy(x => x.StartDate ?? DateTime.MaxValue)
                    .FirstOrDefault()
                ?? gradingPeriods
                    .OrderByDescending(x => x.EndDate ?? DateTime.MinValue)
                    .FirstOrDefault();
        }

        private static string BuildSchoolYearDashboardLabel(SchoolYear schoolYear)
        {
            var dateRange = schoolYear.StartDate.HasValue && schoolYear.EndDate.HasValue
                ? $" ({schoolYear.StartDate.Value:MMM yyyy} - {schoolYear.EndDate.Value:MMM yyyy})"
                : string.Empty;

            return $"{schoolYear.Name}{dateRange} [{schoolYear.Status}]";
        }

        private static string BuildGradingPeriodDashboardLabel(GradingPeriod gradingPeriod)
        {
            var dateRange = gradingPeriod.StartDate.HasValue && gradingPeriod.EndDate.HasValue
                ? $" ({gradingPeriod.StartDate.Value:MMM dd} - {gradingPeriod.EndDate.Value:MMM dd})"
                : string.Empty;

            return $"{gradingPeriod.Name}{dateRange} [{gradingPeriod.Status}]";
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
            BindOperationsModuleButton(btnOpsSubjectEnrollees, OperationsSection.Scheduling, "subject_enrollees", () => new SubjectStudentsWindow(hostedInline: true));
            BindOperationsModuleButton(btnOpsSchedules, OperationsSection.Scheduling, "schedules", () => new SchedulesWindow());
            BindOperationsModuleButton(btnOpsTeacherLoads, OperationsSection.Scheduling, "teacher_loads", () => new TeacherLoadsWindow(hostedInline: true));
            BindOperationsModuleButton(btnOpsRooms, OperationsSection.Scheduling, "rooms", () => new RoomsWindow());
            BindOperationsModuleButton(btnOpsTimeSlots, OperationsSection.Scheduling, "time_slots", () => new TimeSlotsWindow());

            BindOperationsModuleButton(btnOpsStudentAccounts, OperationsSection.AccountsCompliance, "student_accounts", () => new StudentAccountsWindow(_currentUser, dialogOwner: this));
            BindOperationsModuleButton(btnOpsRequirements, OperationsSection.AccountsCompliance, "student_requirements", () => new StudentRequirementsWindow());
            BindOperationsModuleButton(btnOpsArchive, OperationsSection.AccountsCompliance, "archive_center", () => new ArchiveCenterWindow());

            BindOperationsModuleButton(btnOpsBackup, OperationsSection.Maintenance, "backup_restore", () => new BackupRestoreWindow(() => Logout("RESTORE_RELOGIN")));
            BindOperationsModuleButton(btnOpsYearEnd, OperationsSection.Maintenance, "year_end_rollover", () => new YearEndRolloverWindow());

            _opsDefaultLoaders[OperationsSection.MasterData] = () => LoadOperationsModule(OperationsSection.MasterData, btnOpsSchoolSettings, "school_settings", () => new SchoolSettingsWindow());
            _opsDefaultLoaders[OperationsSection.Scheduling] = () => LoadOperationsModule(OperationsSection.Scheduling, btnOpsOfferings, "class_offerings", () => new ClassOfferingsWindow());
            _opsDefaultLoaders[OperationsSection.AccountsCompliance] = () => LoadOperationsModule(OperationsSection.AccountsCompliance, btnOpsStudentAccounts, "student_accounts", () => new StudentAccountsWindow(_currentUser, dialogOwner: this));
            _opsDefaultLoaders[OperationsSection.Maintenance] = () => LoadOperationsModule(OperationsSection.Maintenance, btnOpsBackup, "backup_restore", () => new BackupRestoreWindow(() => Logout("RESTORE_RELOGIN")));
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
                info.Text = $"{sectionLabel}: {moduleCount} module(s) available. Select one from the launch list to open its workspace on this page.";
                return;
            }

            var label = _opsButtonLabels.TryGetValue(selectedButton, out var mapped)
                ? mapped
                : selectedButton.Content?.ToString() ?? "Module";
            info.Text = $"{sectionLabel} active module: {label}. Use the launch list to switch context without leaving the page.";
        }

        private void OpenStudentSearchModal()
        {
            try
            {
                var modal = new StudentSearchModal { Owner = this };
                if (modal.ShowDialog() == true && modal.SelectedStudentId.HasValue)
                {
                    NavigateToStudentDetails(modal.SelectedStudentId.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open student search: {ex.Message}", "Students", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenStudentGradesWindow()
        {
            try
            {
                NavigateToStudentGrades();
            }
            catch (Exception ex)
            {
                AppFeedbackService.ShowError($"Failed to open Student Grades: {ex.Message}", "Student Grades", this);
            }
        }

        private void OpenMasterDataSections()
        {
            NavigateMainTab(5);
            LoadOperationsModule(OperationsSection.MasterData, btnOpsSections, "sections", () => new SectionsWindow());
        }

        private void OpenArchiveCenter()
        {
            NavigateMainTab(7);
            LoadOperationsModule(OperationsSection.AccountsCompliance, btnOpsArchive, "archive_center", () => new ArchiveCenterWindow());
        }

        private void EnsureDefaultOperationsModuleForTab(int tabIndex)
        {
            var section = tabIndex switch
            {
                5 => OperationsSection.MasterData,
                6 => OperationsSection.Scheduling,
                7 => OperationsSection.AccountsCompliance,
                8 => OperationsSection.Maintenance,
                _ => (OperationsSection?)null
            };

            if (!section.HasValue)
            {
                return;
            }

            EnsureDefaultOperationsModuleLoaded(section.Value);
        }

        private void EnsureDefaultOperationsModuleLoaded(OperationsSection section)
        {
            if (_opsHosts.TryGetValue(section, out var host) && host.Content != null)
            {
                return;
            }

            if (_opsDefaultLoaders.TryGetValue(section, out var loadDefault))
            {
                loadDefault();
            }
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

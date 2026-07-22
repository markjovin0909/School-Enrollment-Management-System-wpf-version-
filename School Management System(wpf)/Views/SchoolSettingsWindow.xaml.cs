using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class SchoolSettingsWindow : Window
    {
        private readonly SchoolSettingService _settingService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly SchoolBrandingService _brandingService = new();

        private long? _settingId;
        private string? _selectedLogoSourcePath;
        private string? _existingLogoPath;
        private bool _clearLogoRequested;

        public SchoolSettingsWindow()
        {
            InitializeComponent();

            btnReload.Click += (_, _) => LoadData();
            btnSave.Click += (_, _) => SaveSettings();
            btnBrowseLogo.Click += (_, _) => BrowseLogo();
            btnClearLogo.Click += (_, _) => ClearLogo();

            LoadData();
        }

        private void LoadData()
        {
            var setting = _settingService.GetAll().OrderByDescending(x => x.Id).FirstOrDefault();
            if (setting == null)
            {
                _settingId = null;
                _existingLogoPath = null;
                _selectedLogoSourcePath = null;
                _clearLogoRequested = false;
                txtSchoolName.Text = "School Management System";
                txtSchoolCode.Clear();
                txtAddress.Clear();
                txtPrincipal.Clear();
                txtEnrollmentConfiguration.Text = "Standard enrollment workflow with manual review of submitted student requirements.";

                chkEnrollmentOpen.IsChecked = false;
                dpEnrollmentOpen.SelectedDate = DateTime.Today;
                chkEnrollmentClose.IsChecked = false;
                dpEnrollmentClose.SelectedDate = DateTime.Today;

                txtPrintHeader1.Text = txtSchoolName.Text;
                txtPrintHeader2.Clear();
                txtStudentPrefix.Text = SchoolSettingService.NormalizeStudentNumberPrefix("SMS", string.Empty);
                txtNextStudentNo.Text = "1";
                txtDefaultCapacity.Text = "45";
                txtDefaultGradeLevels.Text = string.Empty;
                RefreshLogoPreview(null);
            }
            else
            {
                _settingId = setting.Id;
                _existingLogoPath = setting.SchoolLogoPath;
                _selectedLogoSourcePath = null;
                _clearLogoRequested = false;
                txtSchoolName.Text = setting.SchoolName;
                txtSchoolCode.Text = setting.SchoolCode;
                txtAddress.Text = setting.SchoolAddress;
                txtPrincipal.Text = setting.PrincipalName;
                txtEnrollmentConfiguration.Text = string.IsNullOrWhiteSpace(setting.EnrollmentConfiguration)
                    ? setting.GradingSetup
                    : setting.EnrollmentConfiguration;

                chkEnrollmentOpen.IsChecked = setting.EnrollmentOpenDate.HasValue;
                dpEnrollmentOpen.SelectedDate = setting.EnrollmentOpenDate?.Date ?? DateTime.Today;
                chkEnrollmentClose.IsChecked = setting.EnrollmentCloseDate.HasValue;
                dpEnrollmentClose.SelectedDate = setting.EnrollmentCloseDate?.Date ?? DateTime.Today;

                txtPrintHeader1.Text = string.IsNullOrWhiteSpace(setting.PrintHeaderLine1) ? setting.SchoolName : setting.PrintHeaderLine1;
                txtPrintHeader2.Text = string.IsNullOrWhiteSpace(setting.PrintHeaderLine2) ? setting.SchoolAddress : setting.PrintHeaderLine2;
                txtStudentPrefix.Text = SchoolSettingService.NormalizeStudentNumberPrefix(
                    setting.StudentNumberPrefix,
                    setting.SchoolCode);
                txtNextStudentNo.Text = (setting.NextStudentNumber > 0 ? setting.NextStudentNumber : 1).ToString();
                txtDefaultCapacity.Text = (setting.DefaultSectionCapacity > 0 ? setting.DefaultSectionCapacity : 45).ToString();
                txtDefaultGradeLevels.Text = FormatDefaultGradeLevels(setting.DefaultGradeLevelIds);
                RefreshLogoPreview(setting.SchoolLogoPath);
            }

            txtStatus.Text = "Loaded.";
        }

        private void SaveSettings()
        {
            if (string.IsNullOrWhiteSpace(txtSchoolName.Text))
            {
                MessageBox.Show("School name is required.", "School Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (chkEnrollmentOpen.IsChecked == true && chkEnrollmentClose.IsChecked == true)
            {
                var open = dpEnrollmentOpen.SelectedDate ?? DateTime.Today;
                var close = dpEnrollmentClose.SelectedDate ?? DateTime.Today;
                if (open.Date > close.Date)
                {
                    MessageBox.Show("Enrollment open date cannot be later than close date.", "School Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (!int.TryParse(txtNextStudentNo.Text.Trim(), out var nextStudentNo) || nextStudentNo <= 0)
            {
                MessageBox.Show("Next student number must be a positive integer.", "School Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtDefaultCapacity.Text.Trim(), out var defaultCapacity) || defaultCapacity <= 0)
            {
                MessageBox.Show("Default section capacity must be a positive integer.", "School Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryResolveDefaultGradeLevels(txtDefaultGradeLevels.Text, out var normalizedGradeLevelIds, out var gradeLevelError))
            {
                MessageBox.Show(gradeLevelError, "School Settings", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var now = DateTime.UtcNow;
            string? previousLogoPath = null;
            SchoolSetting entity;
            if (_settingId.HasValue)
            {
                entity = _settingService.GetById(_settingId.Value) ?? new SchoolSetting();
                if (entity.Id == 0)
                {
                    entity.CreatedAt = now;
                }

                previousLogoPath = entity.SchoolLogoPath;
            }
            else
            {
                entity = new SchoolSetting
                {
                    CreatedAt = now
                };
            }

            var updatedLogoPath = ResolveLogoPathForSave(entity.SchoolLogoPath);

            entity.SchoolName = txtSchoolName.Text.Trim();
            entity.SchoolCode = txtSchoolCode.Text.Trim();
            entity.SchoolAddress = txtAddress.Text.Trim();
            entity.PrincipalName = txtPrincipal.Text.Trim();
            // Keep GradingSetup for legacy readers; enrollment guidance is the primary UI field.
            if (string.IsNullOrWhiteSpace(entity.GradingSetup))
            {
                entity.GradingSetup = txtEnrollmentConfiguration.Text.Trim();
            }
            entity.EnrollmentConfiguration = txtEnrollmentConfiguration.Text.Trim();
            entity.EnrollmentOpenDate = chkEnrollmentOpen.IsChecked == true ? (dpEnrollmentOpen.SelectedDate ?? DateTime.Today).Date : null;
            entity.EnrollmentCloseDate = chkEnrollmentClose.IsChecked == true ? (dpEnrollmentClose.SelectedDate ?? DateTime.Today).Date : null;
            entity.PrintHeaderLine1 = string.IsNullOrWhiteSpace(txtPrintHeader1.Text) ? txtSchoolName.Text.Trim() : txtPrintHeader1.Text.Trim();
            entity.PrintHeaderLine2 = string.IsNullOrWhiteSpace(txtPrintHeader2.Text) ? txtAddress.Text.Trim() : txtPrintHeader2.Text.Trim();
            entity.SchoolLogoPath = updatedLogoPath;
            entity.StudentNumberPrefix = SchoolSettingService.NormalizeStudentNumberPrefix(
                txtStudentPrefix.Text,
                entity.SchoolCode,
                entity.StudentNumberPrefix);
            entity.NextStudentNumber = nextStudentNo;
            entity.DefaultSectionCapacity = defaultCapacity;
            entity.DefaultGradeLevelIds = normalizedGradeLevelIds;
            entity.UpdatedAt = now;

            if (entity.Id == 0)
            {
                _settingService.Create(entity);
                _settingId = entity.Id;
                AuditTrailService.Log("CREATE", "school_settings", entity.Id, null, entity);
            }
            else
            {
                _settingService.Update(entity);
                AuditTrailService.Log("UPDATE", "school_settings", entity.Id, null, entity);
            }

            DeleteSupersededLogo(previousLogoPath, entity.SchoolLogoPath);
            _existingLogoPath = entity.SchoolLogoPath;
            _selectedLogoSourcePath = null;
            _clearLogoRequested = false;
            RefreshLogoPreview(entity.SchoolLogoPath);
            txtStudentPrefix.Text = entity.StudentNumberPrefix;

            // Push settings effects into the live main shell (branding, labels, etc.).
            if (Application.Current?.MainWindow is School_Management_System.MainWindow mainWindow)
            {
                mainWindow.RefreshSchoolSettingsEffects();
            }

            txtStatus.Text = $"Saved at {DateTime.Now:HH:mm:ss}";
            MessageBox.Show(
                "School settings saved. Branding, enrollment defaults, numbering, section capacity, and grade scope are now active across the system.",
                "School Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BrowseLogo()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select School Logo",
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            var dialogOwner = Application.Current?.MainWindow;
            var selected = dialogOwner != null
                ? dialog.ShowDialog(dialogOwner)
                : dialog.ShowDialog();

            if (selected != true)
            {
                return;
            }

            try
            {
                _selectedLogoSourcePath = dialog.FileName;
                _clearLogoRequested = false;
                txtLogoStatus.Text = $"Pending custom logo: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                _selectedLogoSourcePath = null;
                _clearLogoRequested = false;
                RefreshLogoPreview(_existingLogoPath);
                MessageBox.Show(
                    $"The selected file could not be used as a logo.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "School Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void ClearLogo()
        {
            _selectedLogoSourcePath = null;
            _clearLogoRequested = true;
            RefreshLogoPreview(null);
            txtLogoStatus.Text = "Custom logo will be cleared on save. Default logo will be used.";
        }

        private string FormatDefaultGradeLevels(string? rawIds)
        {
            var selectedIds = SchoolSettingService.ParseGradeLevelIds(rawIds);
            if (selectedIds.Count == 0)
            {
                return string.Empty;
            }

            var gradeLevels = _gradeLevelService.GetAll().ToList();
            return string.Join(", ", selectedIds.Select(id =>
            {
                var grade = gradeLevels.FirstOrDefault(x => x.Id == id);
                if (grade == null)
                {
                    return id.ToString();
                }

                return string.IsNullOrWhiteSpace(grade.Code) ? grade.Name : grade.Code;
            }));
        }

        private bool TryResolveDefaultGradeLevels(string input, out string? normalized, out string errorMessage)
        {
            errorMessage = string.Empty;
            normalized = null;

            var raw = (input ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return true;
            }

            var gradeLevels = _gradeLevelService.GetAll().ToList();
            var resolvedIds = new List<long>();
            var tokens = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            foreach (var token in tokens)
            {
                if (long.TryParse(token, out var numericId))
                {
                    var exists = gradeLevels.Any(x => x.Id == numericId);
                    if (!exists)
                    {
                        errorMessage = $"Unknown grade level ID '{token}'.";
                        return false;
                    }

                    resolvedIds.Add(numericId);
                    continue;
                }

                var match = gradeLevels.FirstOrDefault(x =>
                    string.Equals(x.Code, token, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Name, token, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    errorMessage = $"Unknown grade level '{token}'. Use valid grade code, name, or numeric ID.";
                    return false;
                }

                resolvedIds.Add(match.Id);
            }

            normalized = resolvedIds.Count == 0
                ? null
                : SchoolSettingService.NormalizeGradeLevelIds(resolvedIds);
            return true;
        }

        private string? ResolveLogoPathForSave(string? existingLogoPath)
        {
            if (_clearLogoRequested)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(_selectedLogoSourcePath))
            {
                var storedPath = FileStorageService.SaveSchoolLogo(_selectedLogoSourcePath);
                if (string.IsNullOrWhiteSpace(storedPath))
                {
                    throw new InvalidOperationException("The selected logo could not be copied into the app branding folder.");
                }

                return storedPath;
            }

            return existingLogoPath;
        }

        private static void DeleteSupersededLogo(string? previousLogoPath, string? currentLogoPath)
        {
            if (string.IsNullOrWhiteSpace(previousLogoPath))
            {
                return;
            }

            if (string.Equals(previousLogoPath, currentLogoPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            FileStorageService.DeleteSchoolLogo(previousLogoPath);
        }

        private void RefreshLogoPreview(string? logoPath)
        {
            imgSchoolLogo.Source = _brandingService.LoadLogoImage(logoPath);
            if (string.IsNullOrWhiteSpace(logoPath))
            {
                txtLogoStatus.Text = "Using the default bundled logo.";
                return;
            }

            txtLogoStatus.Text = "Using a saved custom logo.";
        }

    }
}

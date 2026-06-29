using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class SchoolSettingsWindow : Window
    {
        private readonly SchoolSettingService _settingService = new();
        private readonly GradeLevelService _gradeLevelService = new();

        private long? _settingId;

        public SchoolSettingsWindow()
        {
            InitializeComponent();

            btnReload.Click += (_, _) => LoadData();
            btnSave.Click += (_, _) => SaveSettings();

            LoadData();
        }

        private void LoadData()
        {
            var setting = _settingService.GetAll().OrderByDescending(x => x.Id).FirstOrDefault();
            if (setting == null)
            {
                _settingId = null;
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
                txtStudentPrefix.Text = "SMS";
                txtNextStudentNo.Text = "1";
                txtDefaultCapacity.Text = "45";
                txtDefaultGradeLevels.Text = string.Empty;
            }
            else
            {
                _settingId = setting.Id;
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
                txtStudentPrefix.Text = string.IsNullOrWhiteSpace(setting.StudentNumberPrefix)
                    ? ResolveStudentNumberPrefix(setting.SchoolCode, setting.StudentNumberPrefix ?? string.Empty)
                    : setting.StudentNumberPrefix;
                txtNextStudentNo.Text = (setting.NextStudentNumber > 0 ? setting.NextStudentNumber : 1).ToString();
                txtDefaultCapacity.Text = (setting.DefaultSectionCapacity > 0 ? setting.DefaultSectionCapacity : 45).ToString();
                txtDefaultGradeLevels.Text = FormatDefaultGradeLevels(setting.DefaultGradeLevelIds);
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
            SchoolSetting entity;
            if (_settingId.HasValue)
            {
                entity = _settingService.GetById(_settingId.Value) ?? new SchoolSetting();
                if (entity.Id == 0)
                {
                    entity.CreatedAt = now;
                }
            }
            else
            {
                entity = new SchoolSetting
                {
                    CreatedAt = now
                };
            }

            entity.SchoolName = txtSchoolName.Text.Trim();
            entity.SchoolCode = txtSchoolCode.Text.Trim();
            entity.SchoolAddress = txtAddress.Text.Trim();
            entity.PrincipalName = txtPrincipal.Text.Trim();
            entity.GradingSetup = txtEnrollmentConfiguration.Text.Trim();
            entity.EnrollmentConfiguration = txtEnrollmentConfiguration.Text.Trim();
            entity.EnrollmentOpenDate = chkEnrollmentOpen.IsChecked == true ? (dpEnrollmentOpen.SelectedDate ?? DateTime.Today).Date : null;
            entity.EnrollmentCloseDate = chkEnrollmentClose.IsChecked == true ? (dpEnrollmentClose.SelectedDate ?? DateTime.Today).Date : null;
            entity.PrintHeaderLine1 = string.IsNullOrWhiteSpace(txtPrintHeader1.Text) ? txtSchoolName.Text.Trim() : txtPrintHeader1.Text.Trim();
            entity.PrintHeaderLine2 = string.IsNullOrWhiteSpace(txtPrintHeader2.Text) ? txtAddress.Text.Trim() : txtPrintHeader2.Text.Trim();
            entity.StudentNumberPrefix = ResolveStudentNumberPrefix(txtStudentPrefix.Text.Trim(), entity.StudentNumberPrefix ?? string.Empty);
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

            txtStatus.Text = $"Saved at {DateTime.Now:HH:mm:ss}";
            MessageBox.Show("School settings saved.", "School Settings", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private static string ResolveStudentNumberPrefix(string schoolCode, string existingPrefix)
        {
            if (!string.IsNullOrWhiteSpace(schoolCode))
            {
                var cleaned = new string(schoolCode.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(cleaned))
                {
                    return cleaned.Length <= 3 ? cleaned : cleaned.Substring(0, 3);
                }
            }

            if (!string.IsNullOrWhiteSpace(existingPrefix))
            {
                return existingPrefix.Trim().ToUpperInvariant();
            }

            return "S";
        }
    }
}

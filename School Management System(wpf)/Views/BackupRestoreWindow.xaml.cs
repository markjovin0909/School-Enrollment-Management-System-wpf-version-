using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class BackupRestoreWindow : Window
    {
        private const int HistoryRows = 30;
        private const string RestoreTypedConfirmation = "RESTORE";

        private readonly BackupRestoreService _service = new();
        private readonly Action? _requestSessionRefresh;
        private BackupRestoreSettings _settings = new();
        private string? _lastLogPath;
        private bool _restoreBusy;
        private string? _restorePreflightCachePath;
        private DateTime _restorePreflightCacheLastWriteUtc;
        private RestorePreflightResult? _restorePreflightCacheResult;

        public BackupRestoreWindow(Action? requestSessionRefresh = null)
        {
            _requestSessionRefresh = requestSessionRefresh;
            InitializeComponent();

            chkZipCompression.Checked += (_, _) => PersistSettings();
            chkZipCompression.Unchecked += (_, _) => PersistSettings();

            btnBrowseBackupFolder.Click += (_, _) => BrowseBackupFolder();
            btnBrowseRestoreFile.Click += (_, _) => BrowseRestoreFile();
            btnBrowseMySqlBinFolder.Click += (_, _) => BrowseMySqlBinFolder();

            btnBackupNow.Click += async (_, _) => await RunBackupAsync();
            btnRestoreNow.Click += async (_, _) => await RunRestoreAsync();
            btnOpenBackupFolder.Click += (_, _) => OpenBackupFolder();
            btnViewLog.Click += (_, _) => ViewLog();
            txtRestoreFile.TextChanged += (_, _) =>
            {
                InvalidateRestorePreflightCache();
                UpdateRestoreExecutionAvailability();
            };
            txtMySqlBinFolder.TextChanged += (_, _) =>
            {
                InvalidateRestorePreflightCache();
                UpdateRestoreExecutionAvailability();
            };
            chkRestoreAcknowledge.Checked += (_, _) => UpdateRestoreExecutionAvailability();
            chkRestoreAcknowledge.Unchecked += (_, _) => UpdateRestoreExecutionAvailability();
            txtRestoreTypeConfirm.TextChanged += (_, _) => UpdateRestoreExecutionAvailability();

            txtBackupFolder.LostFocus += (_, _) => PersistSettings();
            txtMySqlBinFolder.LostFocus += (_, _) => PersistSettings();

            LoadSettings();
            LoadHistory();
            lblBackupStatus.Text = "Ready.";
            lblRestoreStatus.Text = "Ready.";
            UpdateRestoreExecutionAvailability();
        }

        private void LoadSettings()
        {
            _settings = _service.LoadSettings();
            txtBackupFolder.Text = _settings.BackupFolder;
            chkZipCompression.IsChecked = _settings.ZipCompression;
            txtMySqlBinFolder.Text = _settings.MySqlBinFolder;
        }

        private void PersistSettings()
        {
            _settings.BackupFolder = txtBackupFolder.Text.Trim();
            _settings.ZipCompression = chkZipCompression.IsChecked == true;
            _settings.MySqlBinFolder = txtMySqlBinFolder.Text.Trim();
            _service.SaveSettings(_settings);
        }

        private void LoadHistory()
        {
            var rows = _service.LoadHistory(HistoryRows)
                .Select(entry => new HistoryRow
                {
                    Date = entry.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    Action = entry.Action,
                    File = string.IsNullOrWhiteSpace(entry.FilePath) ? "-" : Path.GetFileName(entry.FilePath),
                    Size = entry.FileSizeBytes <= 0 ? "-" : FormatBytes(entry.FileSizeBytes),
                    Status = entry.Status,
                    Notes = entry.Notes
                })
                .ToList();

            gridHistory.ItemsSource = rows;
            _lastLogPath = _service.GetLatestLogFilePath();
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            var unit = 0;

            while (size >= 1024d && unit < units.Length - 1)
            {
                size /= 1024d;
                unit++;
            }

            return $"{size:0.##} {units[unit]}";
        }

        private void BrowseBackupFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select the backup folder"
            };

            if (Directory.Exists(txtBackupFolder.Text))
            {
                dialog.InitialDirectory = txtBackupFolder.Text;
            }

            var owner = AppFeedbackService.ResolveOwner(this, txtBackupFolder);
            if (owner != null ? dialog.ShowDialog(owner) == true : dialog.ShowDialog() == true)
            {
                txtBackupFolder.Text = dialog.FolderName;
                PersistSettings();
            }
        }

        private void BrowseRestoreFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "SQL/ZIP backup files (*.sql;*.zip)|*.sql;*.zip|SQL files (*.sql)|*.sql|ZIP files (*.zip)|*.zip",
                CheckFileExists = true,
                Title = "Select restore source"
            };

            var owner = AppFeedbackService.ResolveOwner(this, txtRestoreFile);
            if (owner != null ? dialog.ShowDialog(owner) == true : dialog.ShowDialog() == true)
            {
                txtRestoreFile.Text = dialog.FileName;
            }
        }

        private void BrowseMySqlBinFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select MySQL bin folder"
            };

            if (Directory.Exists(txtMySqlBinFolder.Text))
            {
                dialog.InitialDirectory = txtMySqlBinFolder.Text;
            }

            var owner = AppFeedbackService.ResolveOwner(this, txtMySqlBinFolder);
            if (owner != null ? dialog.ShowDialog(owner) == true : dialog.ShowDialog() == true)
            {
                txtMySqlBinFolder.Text = dialog.FolderName;
                PersistSettings();
            }
        }

        private async System.Threading.Tasks.Task RunBackupAsync()
        {
            if (string.IsNullOrWhiteSpace(txtBackupFolder.Text))
            {
                MessageBox.Show("Select a backup folder first.", "Backup", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            PersistSettings();
            SetBackupBusy(true);
            lblBackupStatus.Text = "Starting backup...";

            try
            {
                var progress = new Progress<string>(status => lblBackupStatus.Text = status);
                var result = await _service.BackupAsync(new BackupRequest
                {
                    BackupFolder = txtBackupFolder.Text.Trim(),
                    ZipCompression = chkZipCompression.IsChecked == true,
                    MySqlBinFolder = txtMySqlBinFolder.Text.Trim()
                }, progress);

                _lastLogPath = result.LogFilePath;
                lblBackupStatus.Text = result.Summary;
                LoadHistory();

                if (result.Success)
                {
                    MessageBox.Show($"Backup completed.\n\nFile: {result.TargetFilePath}", "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"{result.Summary}\n\nUse 'View Log' for details.", "Backup", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                lblBackupStatus.Text = $"Backup error: {ex.Message}";
                MessageBox.Show(lblBackupStatus.Text, "Backup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBackupBusy(false);
            }
        }

        private async System.Threading.Tasks.Task RunRestoreAsync()
        {
            if (!IsRestoreExecutionReady(out var preflightMessage))
            {
                MessageBox.Show(preflightMessage, "Restore", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "Restore will overwrite data in the configured database.\n\nContinue?",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            PersistSettings();
            SetRestoreBusy(true);
            lblRestoreStatus.Text = "Starting restore...";
            var forceSessionRefresh = false;

            try
            {
                var progress = new Progress<string>(status => lblRestoreStatus.Text = status);
                var result = await _service.RestoreAsync(new RestoreRequest
                {
                    RestoreFilePath = txtRestoreFile.Text.Trim(),
                    MySqlBinFolder = txtMySqlBinFolder.Text.Trim()
                }, progress);

                _lastLogPath = result.LogFilePath;
                lblRestoreStatus.Text = result.Summary;
                LoadHistory();

                if (!result.Success)
                {
                    MessageBox.Show($"{result.Summary}\n\nUse 'View Log' for details.", "Restore", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                forceSessionRefresh = true;
                MessageBox.Show(
                    "Restore completed successfully. The current session must now refresh and re-login to prevent stale cache usage.",
                    "Restore",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                lblRestoreStatus.Text = $"Restore error: {ex.Message}";
                MessageBox.Show(lblRestoreStatus.Text, "Restore", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetRestoreBusy(false);
            }

            if (!forceSessionRefresh)
            {
                return;
            }

            if (_requestSessionRefresh != null)
            {
                _requestSessionRefresh();
                return;
            }

            Close();
        }

        private void SetBackupBusy(bool busy)
        {
            btnBackupNow.IsEnabled = !busy;
            btnBrowseBackupFolder.IsEnabled = !busy;
            chkZipCompression.IsEnabled = !busy;
            progressBackup.IsIndeterminate = busy;
        }

        private void SetRestoreBusy(bool busy)
        {
            _restoreBusy = busy;
            btnBrowseRestoreFile.IsEnabled = !busy;
            chkRestoreAcknowledge.IsEnabled = !busy;
            txtRestoreTypeConfirm.IsEnabled = !busy;
            progressRestore.IsIndeterminate = busy;
            UpdateRestoreExecutionAvailability();
        }

        private bool IsRestoreExecutionReady(out string message)
        {
            if (string.IsNullOrWhiteSpace(txtRestoreFile.Text) || !File.Exists(txtRestoreFile.Text))
            {
                message = "Select a valid restore file first.";
                return false;
            }

            var preflight = GetRestorePreflightResult();
            if (!preflight.Success)
            {
                message = BuildRestorePreflightMessage(preflight, includeBlockingReasons: true);
                return false;
            }

            if (chkRestoreAcknowledge.IsChecked != true)
            {
                message = "Restore preflight passed. Confirm the overwrite acknowledgment before restoring.";
                return false;
            }

            if (!string.Equals((txtRestoreTypeConfirm.Text ?? string.Empty).Trim(), RestoreTypedConfirmation, StringComparison.OrdinalIgnoreCase))
            {
                message = $"Restore preflight passed. Type '{RestoreTypedConfirmation}' to enable restore.";
                return false;
            }

            message = BuildRestorePreflightMessage(preflight, includeBlockingReasons: false);
            return true;
        }

        private void UpdateRestoreExecutionAvailability()
        {
            var ready = IsRestoreExecutionReady(out var message);
            btnRestoreNow.IsEnabled = !_restoreBusy && ready;
            lblRestorePreflight.Text = message;
            lblRestorePreflight.Foreground = ready ? (System.Windows.Media.Brush)FindResource("Brush.Success") : (System.Windows.Media.Brush)FindResource("Brush.Warning");
        }

        private RestorePreflightResult GetRestorePreflightResult()
        {
            var restorePath = (txtRestoreFile.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(restorePath) || !File.Exists(restorePath))
            {
                return RestorePreflightResult.Fail("Restore file not selected.");
            }

            var info = new FileInfo(restorePath);
            if (_restorePreflightCacheResult != null &&
                string.Equals(_restorePreflightCachePath, restorePath, StringComparison.OrdinalIgnoreCase) &&
                _restorePreflightCacheLastWriteUtc == info.LastWriteTimeUtc)
            {
                return _restorePreflightCacheResult;
            }

            var result = _service.EvaluateRestorePreflight(new RestoreRequest
            {
                RestoreFilePath = restorePath,
                MySqlBinFolder = txtMySqlBinFolder.Text.Trim()
            });

            _restorePreflightCachePath = restorePath;
            _restorePreflightCacheLastWriteUtc = info.LastWriteTimeUtc;
            _restorePreflightCacheResult = result;
            return result;
        }

        private static string BuildRestorePreflightMessage(RestorePreflightResult result, bool includeBlockingReasons)
        {
            var lines = new System.Collections.Generic.List<string>
            {
                result.Summary
            };

            if (result.Checks.Count > 0)
            {
                foreach (var check in result.Checks.Take(4))
                {
                    lines.Add($"- [{check.Outcome}] {check.Message}");
                }
            }

            if (includeBlockingReasons && result.BlockingReasons.Count > 0)
            {
                lines.Add("Blocking reasons:");
                foreach (var blocker in result.BlockingReasons.Take(4))
                {
                    lines.Add($"- {blocker}");
                }
            }

            if (result.Warnings.Count > 0)
            {
                lines.Add("Warnings:");
                foreach (var warning in result.Warnings.Take(3))
                {
                    lines.Add($"- {warning}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }

        private void InvalidateRestorePreflightCache()
        {
            _restorePreflightCachePath = null;
            _restorePreflightCacheLastWriteUtc = default;
            _restorePreflightCacheResult = null;
        }

        private void OpenBackupFolder()
        {
            var folder = txtBackupFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(folder))
            {
                MessageBox.Show("No backup folder selected.", "Open Folder", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Directory.CreateDirectory(folder);
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open folder: {ex.Message}", "Open Folder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewLog()
        {
            var logPath = !string.IsNullOrWhiteSpace(_lastLogPath) && File.Exists(_lastLogPath)
                ? _lastLogPath
                : _service.GetLatestLogFilePath();

            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            {
                MessageBox.Show("No backup/restore log file found yet.", "View Log", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open log file: {ex.Message}", "View Log", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private sealed class HistoryRow
        {
            public string Date { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string File { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
        }
    }
}

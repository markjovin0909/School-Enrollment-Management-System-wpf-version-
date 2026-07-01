using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class LoginWindow : Window
    {
        private const string RememberedUsernameKey = "login.remembered.username";
        private readonly SchoolBrandingService _brandingService = new();

        public LoginWindow()
        {
            InitializeComponent();
            btnLogin.Click += (_, _) => SignIn();
            btnForgotPassword.Click += (_, _) => OpenRecovery();
            btnDatabaseSettings.Click += (_, _) => OpenDatabaseSettings();
            btnTestDbConnection.Click += async (_, _) => await TestConnectionAsync();
            chkShowPassword.Checked += (_, _) => TogglePasswordVisibility(true);
            chkShowPassword.Unchecked += (_, _) => TogglePasswordVisibility(false);
            Loaded += (_, _) =>
            {
                ApplyBranding();
                LoadRememberedUsername();
            };
        }

        private void TogglePasswordVisibility(bool visible)
        {
            if (visible)
            {
                txtPasswordVisible.Text = pwdPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                pwdPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Focus();
                txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
                return;
            }

            pwdPassword.Password = txtPasswordVisible.Text;
            pwdPassword.Visibility = Visibility.Visible;
            txtPasswordVisible.Visibility = Visibility.Collapsed;
            pwdPassword.Focus();
        }

        private string ReadPassword()
        {
            return chkShowPassword.IsChecked == true ? txtPasswordVisible.Text : pwdPassword.Password;
        }

        private void SignIn()
        {
            SetStatus(string.Empty, string.Empty);

            var username = txtUsername.Text.Trim();
            var password = ReadPassword();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Missing credentials", "Username and password are required.", "ERROR");
                return;
            }

            var auth = new AuthService();
            var result = auth.Authenticate(username, password);
            if (!result.Success || result.Data == null)
            {
                var isLockout = (result.Message ?? string.Empty).Contains("lock", StringComparison.OrdinalIgnoreCase);
                SetStatus(
                    isLockout ? "Account locked" : "Sign-in failed",
                    result.Message,
                    isLockout ? "WARNING" : "ERROR");
                return;
            }

            SessionContext.CurrentUser = result.Data;

            if (result.Data.Role != UserRole.SUPERADMIN)
            {
                SetStatus("Access denied", "Only SUPERADMIN accounts are supported in this portal.", "WARNING");
                MessageBox.Show("Only SUPERADMIN accounts are supported in this portal.", "Access", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                PersistRememberedUsername(username);
                SetStatus("Sign-in successful", "Opening notifications...", "SUCCESS");

                var app = Application.Current;
                var previousShutdownMode = app.ShutdownMode;
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var notifications = new NotificationsWindow(result.Data) { Owner = this };
                if (notifications.ShowDialog() != true)
                {
                    app.ShutdownMode = previousShutdownMode;
                    SetStatus("Sign-in paused", "Notifications were closed before entering the dashboard.", "WARNING");
                    return;
                }

                var main = new MainWindow(result.Data);
                app.MainWindow = main;
                main.Show();

                app.ShutdownMode = previousShutdownMode;
                Close();
            }
            catch (Exception ex)
            {
                SessionContext.Clear();
                SetStatus("Startup failed", "Main window failed to open.", "ERROR");
                MessageBox.Show(
                    $"Sign-in succeeded, but the main window failed to open.{Environment.NewLine}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}Inner: {ex.InnerException?.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenRecovery()
        {
            var window = new AccountRecoveryWindow { Owner = this };
            window.ShowDialog();
        }

        private void OpenDatabaseSettings()
        {
            var db = new DatabaseConfigurationWindow { Owner = this };
            db.ShowDialog();
        }

        private async Task TestConnectionAsync()
        {
            SetStatus(string.Empty, string.Empty);
            btnTestDbConnection.IsEnabled = false;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            try
            {
                var canConnect = await Task.Run(() =>
                {
                    using var db = new AppDbContext();
                    return db.Database.CanConnect();
                });

                if (canConnect)
                {
                    SetStatus("Connection successful", "Database connection is available for the current environment.", "SUCCESS");
                    MessageBox.Show("Database connection successful.", "Database Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                SetStatus("Connection failed", "Check server, database name, credentials, and network.", "WARNING");
                MessageBox.Show("Database connection failed. Check server, database name, credentials, and network.", "Database Connection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                SetStatus("Connection failed", ex.Message, "ERROR");
                MessageBox.Show($"Database connection failed: {ex.Message}", "Database Connection", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                btnTestDbConnection.IsEnabled = true;
            }
        }

        private void LoadRememberedUsername()
        {
            if (Application.Current.Properties[RememberedUsernameKey] is string remembered &&
                !string.IsNullOrWhiteSpace(remembered))
            {
                txtUsername.Text = remembered;
                chkRememberMe.IsChecked = true;
            }
        }

        private void PersistRememberedUsername(string username)
        {
            if (chkRememberMe.IsChecked == true)
            {
                Application.Current.Properties[RememberedUsernameKey] = username;
                return;
            }

            Application.Current.Properties.Remove(RememberedUsernameKey);
        }

        private void ApplyBranding()
        {
            var branding = _brandingService.GetCurrentBranding();
            Title = branding.SchoolName;
            txtSchoolTitle.Text = branding.SchoolName;
            imgPrimaryLogo.Source = branding.LogoImage;
            imgWatermarkLogo.Source = branding.LogoImage;
        }

        private void SetStatus(string? title, string? message, string? statusType = "")
        {
            lblStatusTitle.Text = title ?? string.Empty;
            lblStatus.Text = message ?? string.Empty;
            txtStatusType.Text = statusType ?? string.Empty;
        }
    }
}

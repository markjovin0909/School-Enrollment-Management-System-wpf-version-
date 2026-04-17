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
        public LoginWindow()
        {
            InitializeComponent();
            btnLogin.Click += (_, _) => SignIn();
            btnForgotPassword.Click += (_, _) => OpenRecovery();
            btnDatabaseSettings.Click += (_, _) => OpenDatabaseSettings();
            btnTestDbConnection.Click += async (_, _) => await TestConnectionAsync();
            chkShowPassword.Checked += (_, _) => TogglePasswordVisibility(true);
            chkShowPassword.Unchecked += (_, _) => TogglePasswordVisibility(false);
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
            lblError.Text = string.Empty;

            var username = txtUsername.Text.Trim();
            var password = ReadPassword();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Username and password are required.";
                return;
            }

            var auth = new AuthService();
            var result = auth.Authenticate(username, password);
            if (!result.Success || result.Data == null)
            {
                lblError.Text = result.Message;
                return;
            }

            SessionContext.CurrentUser = result.Data;

            if (result.Data.Role != UserRole.SUPERADMIN)
            {
                MessageBox.Show("Only SUPERADMIN accounts are supported in this portal.", "Access", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var main = new MainWindow(result.Data);
            Application.Current.MainWindow = main;
            main.Show();
            Close();
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
            lblError.Text = string.Empty;
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
                    MessageBox.Show("Database connection successful.", "Database Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                MessageBox.Show("Database connection failed. Check server, database name, credentials, and network.", "Database Connection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database connection failed: {ex.Message}", "Database Connection", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                btnTestDbConnection.IsEnabled = true;
            }
        }
    }
}

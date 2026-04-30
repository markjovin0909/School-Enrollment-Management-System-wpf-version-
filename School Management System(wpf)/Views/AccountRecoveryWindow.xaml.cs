using System.Windows;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class AccountRecoveryWindow : Window
    {
        public AccountRecoveryWindow()
        {
            InitializeComponent();
            btnRequestRecovery.Click += (_, _) => RequestRecovery();
            btnVerify.Click += (_, _) => VerifyRecovery();
            btnResetPassword.Click += (_, _) => Recover();
            btnCancel.Click += (_, _) => Close();
        }

        private void RequestRecovery()
        {
            SetStatus("Recovery requested", "Use your configured school code as the recovery code, then verify identity and reset the password.", "INFO");
        }

        private void VerifyRecovery()
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtIdentity.Text) ||
                string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                SetStatus("Verification blocked", "Complete username, recovery code, and verification phrase first.", "WARNING");
                return;
            }

            SetStatus("Verification ready", "Identity values are present. You can proceed with password reset.", "SUCCESS");
        }

        private void Recover()
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtIdentity.Text) ||
                string.IsNullOrWhiteSpace(txtLastName.Text) ||
                string.IsNullOrWhiteSpace(txtNewPassword.Password))
            {
                SetStatus("Missing fields", "Complete all required fields.", "WARNING");
                MessageBox.Show("Complete all required fields.", "Recovery", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtNewPassword.Password != txtConfirmPassword.Password)
            {
                SetStatus("Confirmation mismatch", "Password confirmation does not match.", "WARNING");
                MessageBox.Show("Password confirmation does not match.", "Recovery", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var service = new AccountSecurityService();
            var result = service.RecoverPassword(
                txtUsername.Text.Trim(),
                UserRole.SUPERADMIN,
                txtIdentity.Text.Trim(),
                txtLastName.Text.Trim(),
                txtNewPassword.Password);

            if (!result.Success)
            {
                SetStatus("Recovery failed", result.Message, "WARNING");
                MessageBox.Show(result.Message, "Recovery", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetStatus("Recovery successful", "Password recovery successful. You can now sign in.", "SUCCESS");
            MessageBox.Show("Password recovery successful. You can now sign in.", "Recovery", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void SetStatus(string title, string message, string kind)
        {
            txtRecoveryStatusTitle.Text = title ?? string.Empty;
            txtRecoveryStatus.Text = message ?? string.Empty;
            recoveryStatusBanner.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
            recoveryStatusBanner.Style = kind switch
            {
                "SUCCESS" => (Style)FindResource("SuccessBanner"),
                "WARNING" => (Style)FindResource("WarningBanner"),
                "ERROR" => (Style)FindResource("ErrorBanner"),
                _ => (Style)FindResource("InfoBanner")
            };
        }
    }
}

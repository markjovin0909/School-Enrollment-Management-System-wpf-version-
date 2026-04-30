using System.Windows;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly long _userId;
        private readonly bool _requireCurrent;

        public ChangePasswordWindow(long userId, bool requireCurrent = true)
        {
            InitializeComponent();
            _userId = userId;
            _requireCurrent = requireCurrent;

            if (!_requireCurrent)
            {
                lblCurrent.Visibility = Visibility.Collapsed;
                txtCurrent.Visibility = Visibility.Collapsed;
                txtHeaderTitle.Text = "Reset Password";
                txtHeaderSubtitle.Text = "Administrator reset mode is active. Set a new password without providing the current password.";
            }
            else
            {
                txtHeaderTitle.Text = "Change Password";
                txtHeaderSubtitle.Text = "Enter the current password, then provide a compliant new password.";
            }

            btnSave.Click += (_, _) => SaveChange();
            btnCancel.Click += (_, _) => Close();
        }

        private void SaveChange()
        {
            if (string.IsNullOrWhiteSpace(txtNew.Password) || txtNew.Password != txtConfirm.Password)
            {
                SetStatus("Confirmation mismatch", "New password and confirmation must match.", "WARNING");
                MessageBox.Show("New password and confirmation must match.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var passwordValidation = PasswordPolicyService.Validate(txtNew.Password);
            if (!passwordValidation.Success)
            {
                SetStatus("Password policy failed", passwordValidation.Message, "WARNING");
                MessageBox.Show(passwordValidation.Message, "Change Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var service = new AccountSecurityService();
            var result = _requireCurrent
                ? service.ChangePassword(_userId, txtCurrent.Password, txtNew.Password)
                : service.ResetPassword(_userId, txtNew.Password);
            if (!result.Success)
            {
                SetStatus("Update failed", result.Message, "WARNING");
                MessageBox.Show(result.Message, "Change Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetStatus("Password updated", "The password was updated successfully.", "SUCCESS");
            MessageBox.Show("Password updated.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void SetStatus(string title, string message, string kind)
        {
            txtPasswordStatusTitle.Text = title ?? string.Empty;
            txtPasswordStatus.Text = message ?? string.Empty;
            passwordStatusBanner.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
            passwordStatusBanner.Style = kind switch
            {
                "SUCCESS" => (Style)FindResource("SuccessBanner"),
                "WARNING" => (Style)FindResource("WarningBanner"),
                "ERROR" => (Style)FindResource("ErrorBanner"),
                _ => (Style)FindResource("InfoBanner")
            };
        }
    }
}

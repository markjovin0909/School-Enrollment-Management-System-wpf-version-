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
            btnRecover.Click += (_, _) => Recover();
            btnCancel.Click += (_, _) => Close();
        }

        private void Recover()
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtIdentity.Text) ||
                string.IsNullOrWhiteSpace(txtLastName.Text) ||
                string.IsNullOrWhiteSpace(txtNewPassword.Password))
            {
                MessageBox.Show("Complete all required fields.", "Recovery", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtNewPassword.Password != txtConfirmPassword.Password)
            {
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
                MessageBox.Show(result.Message, "Recovery", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Password recovery successful. You can now sign in.", "Recovery", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
    }
}

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
            }

            btnSave.Click += (_, _) => SaveChange();
            btnCancel.Click += (_, _) => Close();
        }

        private void SaveChange()
        {
            if (string.IsNullOrWhiteSpace(txtNew.Password) || txtNew.Password != txtConfirm.Password)
            {
                MessageBox.Show("New password and confirmation must match.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var passwordValidation = PasswordPolicyService.Validate(txtNew.Password);
            if (!passwordValidation.Success)
            {
                MessageBox.Show(passwordValidation.Message, "Change Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var service = new AccountSecurityService();
            var result = _requireCurrent
                ? service.ChangePassword(_userId, txtCurrent.Password, txtNew.Password)
                : service.ResetPassword(_userId, txtNew.Password);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Change Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Password updated.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
    }
}

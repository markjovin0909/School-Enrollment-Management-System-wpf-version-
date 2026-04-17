using System;
using System.Windows;
using System.Windows.Media;
using School_Management_System.Configuration;

namespace School_Management_System.Views
{
    public partial class DatabaseConfigurationWindow : Window
    {
        private readonly bool _startupFlow;

        public DatabaseConfigurationWindow(bool startupFlow = false)
        {
            _startupFlow = startupFlow;
            InitializeComponent();
            cmbEnvironment.ItemsSource = Enum.GetValues(typeof(DatabaseConfig.Environment));
            btnValidate.Click += (_, _) => ValidateCurrentValues();
            btnSave.Click += (_, _) => SaveConfiguration();
            btnClose.Click += (_, _) =>
            {
                if (_startupFlow)
                {
                    DialogResult = false;
                }

                Close();
            };
            LoadFromConfiguration();
        }

        private void LoadFromConfiguration()
        {
            try
            {
                cmbEnvironment.SelectedItem = DatabaseConfig.ActiveEnvironment;
                txtLocal.Text = DatabaseConfig.GetConnectionString(DatabaseConfig.Environment.Local);
                txtRemote.Text = DatabaseConfig.GetConnectionString(DatabaseConfig.Environment.Remote);
                txtOnline.Text = DatabaseConfig.GetConnectionString(DatabaseConfig.Environment.Online);
                SetStatus("Loaded current settings.", true);
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
            }
        }

        private void ValidateCurrentValues()
        {
            if (cmbEnvironment.SelectedItem is not DatabaseConfig.Environment env)
            {
                SetStatus("Please select an active environment.", false);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtLocal.Text) || string.IsNullOrWhiteSpace(txtRemote.Text) || string.IsNullOrWhiteSpace(txtOnline.Text))
            {
                SetStatus("All three connection strings are required.", false);
                return;
            }

            var effective = env switch
            {
                DatabaseConfig.Environment.Local => txtLocal.Text,
                DatabaseConfig.Environment.Remote => txtRemote.Text,
                _ => txtOnline.Text
            };

            if (!effective.Contains("server=", StringComparison.OrdinalIgnoreCase) &&
                !effective.Contains("data source=", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("Active environment connection string looks invalid (missing server/data source).", false);
                return;
            }

            SetStatus("Configuration values look valid. Click Save to apply.", true);
        }

        private void SaveConfiguration()
        {
            if (cmbEnvironment.SelectedItem is not DatabaseConfig.Environment env)
            {
                SetStatus("Please select an active environment.", false);
                return;
            }

            try
            {
                DatabaseConfig.Save(env, txtLocal.Text.Trim(), txtRemote.Text.Trim(), txtOnline.Text.Trim());
                SetStatus("Database settings saved. New connections will use the selected environment.", true);
                MessageBox.Show(
                    "Database settings were saved successfully.",
                    "Database Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                if (_startupFlow)
                {
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
                MessageBox.Show(ex.Message, "Database Settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetStatus(string message, bool success)
        {
            txtStatus.Text = message;
            txtStatus.Foreground = success
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF166534"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF991B1B"));
        }
    }
}

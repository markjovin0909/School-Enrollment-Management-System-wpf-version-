using System;
using System.Windows;
using School_Management_System.Services;
using School_Management_System.Views;

namespace School_Management_System
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Apply official eTinun-an product branding when settings still use legacy defaults.
                try
                {
                    new SchoolSettingService().EnsureProductBrandingDefaults();
                }
                catch
                {
                    // Database may not be reachable at first launch; branding still falls back in UI.
                }

                var login = new LoginWindow();
                MainWindow = login;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                login.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Fatal startup error:\n\n{ex.Message}\n\nThe application will now close.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}

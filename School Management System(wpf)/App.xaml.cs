using System;
using System.Windows;
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

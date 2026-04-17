using System.Reflection;
using Microsoft.Win32;

namespace System.Windows.Forms
{
    // WPF compatibility shim for backend files shared with the WinForms project.
    internal static class Application
    {
        public static string ProductName =>
            Assembly.GetEntryAssembly()?.GetName().Name ?? "School Management System";
    }

    internal enum DialogResult
    {
        Cancel = 0,
        OK = 1
    }

    internal enum MessageBoxButtons
    {
        OK = 0
    }

    internal enum MessageBoxIcon
    {
        Information = 0,
        Warning = 1,
        Error = 2
    }

    internal interface IMessageFilter
    {
        bool PreFilterMessage(ref Message m);
    }

    internal struct Message
    {
        public int Msg { get; set; }
    }

    internal sealed class SaveFileDialog : IDisposable
    {
        private readonly Microsoft.Win32.SaveFileDialog _inner = new();

        public string Filter
        {
            get => _inner.Filter;
            set => _inner.Filter = value;
        }

        public string FileName
        {
            get => _inner.FileName;
            set => _inner.FileName = value;
        }

        public DialogResult ShowDialog()
        {
            return _inner.ShowDialog() == true ? DialogResult.OK : DialogResult.Cancel;
        }

        public void Dispose()
        {
            // Nothing to dispose. Included for WinForms-compatible usage.
        }
    }

    internal static class MessageBox
    {
        public static void Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            _ = buttons;
            var image = icon switch
            {
                MessageBoxIcon.Warning => System.Windows.MessageBoxImage.Warning,
                MessageBoxIcon.Error => System.Windows.MessageBoxImage.Error,
                _ => System.Windows.MessageBoxImage.Information
            };

            System.Windows.MessageBox.Show(text, caption, System.Windows.MessageBoxButton.OK, image);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace School_Management_System
{
    public partial class MainWindow
    {
        private void ShowValidationSummary(Border host, TextBlock messageBlock, IEnumerable<string> errors)
        {
            var items = errors
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            if (items.Count == 0)
            {
                host.Visibility = Visibility.Collapsed;
                messageBlock.Text = string.Empty;
                return;
            }

            messageBlock.Text = string.Join(System.Environment.NewLine, items.Select(x => $"- {x}"));
            host.Visibility = Visibility.Visible;
        }

        private static void HideValidationSummary(Border host, TextBlock messageBlock)
        {
            host.Visibility = Visibility.Collapsed;
            messageBlock.Text = string.Empty;
        }

        private void SetInputValidationState(Control control, bool hasError)
        {
            if (control == null)
            {
                return;
            }

            var borderBrush = hasError
                ? (Brush)FindResource("Brush.Danger")
                : (Brush)FindResource("Brush.Border");

            control.BorderBrush = borderBrush;
            control.ToolTip = hasError ? "Please review this field." : null;
        }
    }
}

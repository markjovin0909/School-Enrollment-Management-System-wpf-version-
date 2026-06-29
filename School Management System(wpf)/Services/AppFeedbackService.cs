using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Markup;

namespace School_Management_System.Services
{
    internal static class AppFeedbackService
    {
        internal static void ShowSuccess(string message, string title, Window? owner = null)
        {
            Show(message, title, MessageBoxImage.Information, owner);
        }

        internal static void ShowInfo(string message, string title, Window? owner = null)
        {
            Show(message, title, MessageBoxImage.Information, owner);
        }

        internal static void ShowWarning(string message, string title, Window? owner = null)
        {
            Show(message, title, MessageBoxImage.Warning, owner);
        }

        internal static void ShowError(string message, string title, Window? owner = null)
        {
            Show(message, title, MessageBoxImage.Error, owner);
        }

        internal static void ShowError(string message, Exception exception, string title, Window? owner = null)
        {
            Show(FormatExceptionMessage(message, exception), title, MessageBoxImage.Error, owner);
        }

        internal static void ShowDetailedWarning(string message, IEnumerable<string>? errors, string title, Window? owner = null)
        {
            Show(FormatDetailedMessage(message, errors), title, MessageBoxImage.Warning, owner);
        }

        internal static bool Confirm(string message, string title, Window? owner = null)
        {
            owner = NormalizeOwner(owner);
            var result = owner != null
                ? MessageBox.Show(owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
                : MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);

            return result == MessageBoxResult.Yes;
        }

        internal static bool? ShowOwnedDialog(Window dialog, Window? logicalOwner, params DependencyObject?[] anchors)
        {
            var owner = ResolveOwner(logicalOwner, anchors);
            if (owner != null && !ReferenceEquals(owner, dialog))
            {
                dialog.Owner = owner;
            }

            return dialog.ShowDialog();
        }

        internal static Window? ResolveOwner(Window? logicalOwner, params DependencyObject?[] anchors)
        {
            foreach (var anchor in anchors)
            {
                var window = anchor == null ? null : Window.GetWindow(anchor);
                if (window != null)
                {
                    return window;
                }
            }

            if (logicalOwner != null && logicalOwner.IsLoaded && logicalOwner.Visibility == Visibility.Visible)
            {
                return logicalOwner;
            }

            return Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(w => w.IsActive && w.Visibility == Visibility.Visible)
                ?? Application.Current?.MainWindow;
        }

        internal static string FormatDetailedMessage(string message, IEnumerable<string>? errors)
        {
            var details = errors?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
            if (details.Count == 0)
            {
                return message;
            }

            return $"{message}{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, details.Select(x => $"- {x}"))}";
        }

        internal static string FormatExceptionMessage(string message, Exception exception)
        {
            var details = DescribeException(exception);
            return details.Count == 0
                ? message
                : $"{message}{Environment.NewLine}{Environment.NewLine}{string.Join(Environment.NewLine, details.Select(x => $"- {x}"))}";
        }

        internal static List<string> DescribeException(Exception? exception)
        {
            var details = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var current = exception;
            var depth = 0;

            while (current != null)
            {
                var prefix = depth == 0 ? "Error" : $"Inner {depth}";
                var message = current.Message?.Trim();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    AddDetail($"{prefix}: {message}");
                }

                if (current is XamlParseException xamlEx)
                {
                    if (xamlEx.LineNumber > 0 || xamlEx.LinePosition > 0)
                    {
                        AddDetail($"XAML location: line {xamlEx.LineNumber}, position {xamlEx.LinePosition}");
                    }

                    if (!string.IsNullOrWhiteSpace(xamlEx.BaseUri?.ToString()))
                    {
                        AddDetail($"XAML source: {xamlEx.BaseUri}");
                    }
                }

                current = current.InnerException;
                depth++;
            }

            return details;

            void AddDetail(string detail)
            {
                if (seen.Add(detail))
                {
                    details.Add(detail);
                }
            }
        }

        private static void Show(string message, string title, MessageBoxImage image, Window? owner)
        {
            owner = NormalizeOwner(owner);
            if (owner != null)
            {
                MessageBox.Show(owner, message, title, MessageBoxButton.OK, image);
                return;
            }

            MessageBox.Show(message, title, MessageBoxButton.OK, image);
        }

        private static Window? NormalizeOwner(Window? owner)
        {
            if (owner == null)
            {
                return null;
            }

            var resolved = ResolveOwner(owner);
            return resolved ?? owner;
        }
    }
}

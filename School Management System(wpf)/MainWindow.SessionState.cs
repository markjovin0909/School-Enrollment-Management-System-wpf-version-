using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Threading;

namespace School_Management_System
{
    public partial class MainWindow
    {
        private readonly Dictionary<string, string> _sessionViewState = new(StringComparer.OrdinalIgnoreCase);

        private void SetSessionState(string key, string? value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                _sessionViewState.Remove(key);
                return;
            }

            _sessionViewState[key] = value.Trim();
        }

        private string GetSessionState(string key, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return fallback;
            }

            return _sessionViewState.TryGetValue(key, out var value) ? value : fallback;
        }

        private long? GetSessionStateLong(string key)
        {
            var raw = GetSessionState(key);
            return long.TryParse(raw, out var id) ? id : null;
        }

        private void SetSessionStateLong(string key, long? value)
        {
            SetSessionState(key, value.HasValue ? value.Value.ToString() : null);
        }

        private void ApplyGridSort(string key, DataView view)
        {
            var sort = GetSessionState($"{key}.sort");
            if (string.IsNullOrWhiteSpace(sort))
            {
                return;
            }

            try
            {
                view.Sort = sort;
            }
            catch
            {
                // Invalid sort expression from stale state; ignore.
            }
        }

        private void CaptureGridSort(string key, DataGrid grid)
        {
            if (grid.ItemsSource is DataView dataView)
            {
                SetSessionState($"{key}.sort", dataView.Sort);
            }
        }

        private void WireGridSortPersistence(DataGrid grid, string key)
        {
            grid.Sorting += (_, _) =>
            {
                Dispatcher.BeginInvoke(
                    new Action(() => CaptureGridSort(key, grid)),
                    DispatcherPriority.Background);
            };
        }

        private static T? ResolveById<T>(IEnumerable<T> values, long? id, Func<T, long> idSelector) where T : class
        {
            if (!id.HasValue || values == null)
            {
                return null;
            }

            return values.FirstOrDefault(x => idSelector(x) == id.Value);
        }
    }
}

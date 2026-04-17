using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class ArchiveCenterWindow : Window
    {
        private readonly ArchiveRecordService _archiveService = new();
        private DataTable _table = new();
        private long? _selectedId;

        public ArchiveCenterWindow()
        {
            InitializeComponent();

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            chkShowRestored.Checked += (_, _) => LoadData();
            chkShowRestored.Unchecked += (_, _) => LoadData();
            btnRefresh.Click += (_, _) => LoadData();
            btnRestore.Click += (_, _) => RestoreSelected();
            gridRecords.SelectionChanged += GridRecords_SelectionChanged;

            LoadData();
        }

        private void LoadData()
        {
            var records = _archiveService.GetAll()
                .OrderByDescending(x => x.DeletedAt)
                .ToList();

            if (chkShowRestored.IsChecked != true)
            {
                records = records.Where(x => !x.IsRestored).ToList();
            }

            _table = new DataTable();
            _table.Columns.Add("Id", typeof(long));
            _table.Columns.Add("Deleted At");
            _table.Columns.Add("Entity");
            _table.Columns.Add("Original ID");
            _table.Columns.Add("Restored");
            _table.Columns.Add("Notes");

            foreach (var record in records)
            {
                _table.Rows.Add(
                    record.Id,
                    record.DeletedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    record.EntityType,
                    record.OriginalEntityId?.ToString() ?? "-",
                    record.IsRestored ? "Yes" : "No",
                    record.Notes ?? string.Empty);
            }

            gridRecords.ItemsSource = _table.DefaultView;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var term = (txtSearch.Text ?? string.Empty).Trim().Replace("'", "''");
            _table.DefaultView.RowFilter = string.IsNullOrWhiteSpace(term)
                ? string.Empty
                : $"Entity LIKE '%{term}%' OR [Original ID] LIKE '%{term}%' OR Notes LIKE '%{term}%'";

            _selectedId = null;
            gridRecords.SelectedItem = null;
        }

        private void GridRecords_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridRecords.SelectedItem is not DataRowView row)
            {
                _selectedId = null;
                return;
            }

            _selectedId = row.Row.Field<long>("Id");
        }

        private void RestoreSelected()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select an archive record first.", "Archive Center", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var previewResult = _archiveService.BuildRestoreImpactPreview(_selectedId.Value);
            if (!previewResult.Success || previewResult.Data == null)
            {
                MessageBox.Show(previewResult.Message, "Archive Center", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var preview = previewResult.Data;
            var previewMessage = BuildRestorePreviewMessage(preview);
            if (!preview.CanProceed)
            {
                MessageBox.Show(
                    previewMessage,
                    "Restore Blocked",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"{previewMessage}\n\nProceed with restore?",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var result = _archiveService.Restore(_selectedId.Value);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Archive Center", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show(result.Message, "Archive Center", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadData();
        }

        private static string BuildRestorePreviewMessage(ArchiveRestoreImpactPreview preview)
        {
            var lines = new System.Collections.Generic.List<string>
            {
                $"Entity: {preview.EntityType}",
                $"Original ID: {(preview.OriginalEntityId.HasValue ? preview.OriginalEntityId.Value.ToString() : "-")}",
                $"Restore strategy: {preview.RestoreStrategy}",
                $"Dependent records: {preview.TotalDependencyCount}"
            };

            if (preview.Dependencies.Count > 0)
            {
                lines.Add("Dependency impact:");
                foreach (var item in preview.Dependencies.Take(8))
                {
                    lines.Add($"- {item.DependentEntity} via {item.Relation}: {item.Count}");
                }
            }

            if (preview.Warnings.Count > 0)
            {
                lines.Add("Warnings:");
                foreach (var warning in preview.Warnings)
                {
                    lines.Add($"- {warning}");
                }
            }

            if (preview.BlockingReasons.Count > 0)
            {
                lines.Add("Blocking reasons:");
                foreach (var reason in preview.BlockingReasons)
                {
                    lines.Add($"- {reason}");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}

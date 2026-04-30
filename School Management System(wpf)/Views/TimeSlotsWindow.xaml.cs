using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class TimeSlotsWindow : Window
    {
        private readonly TimeSlotService _service = new();
        private readonly bool _createOnly;
        private DataTable _table = CreateTableSchema();
        private long? _selectedId;

        public TimeSlotsWindow(bool createOnly = false)
        {
            _createOnly = createOnly;
            InitializeComponent();

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            gridTimeSlots.AutoGeneratingColumn += (_, e) =>
            {
                if (e.PropertyName == "Id")
                {
                    e.Cancel = true;
                }
            };
            gridTimeSlots.SelectionChanged += GridTimeSlots_SelectionChanged;
            btnNew.Click += (_, _) => OpenCreateWindow();
            btnRefresh.Click += (_, _) => LoadData();
            btnAdd.Click += (_, _) =>
            {
                if (_createOnly)
                {
                    AddTimeSlot();
                }
                else
                {
                    OpenCreateWindow();
                }
            };
            btnSave.Click += (_, _) => SaveTimeSlot();
            btnDelete.Click += (_, _) => DeleteTimeSlot();
            btnClear.Click += (_, _) =>
            {
                if (_createOnly)
                {
                    Close();
                }
                else
                {
                    ClearEditor();
                }
            };
            btnExport.Click += (_, _) => CsvExportService.SaveDataTable(_table, "time_slots.csv");

            ClearEditor();
            if (_createOnly)
            {
                ConfigureCreateMode();
            }
            else
            {
                LoadData();
            }
        }

        private void OpenCreateWindow()
        {
            var window = new TimeSlotsWindow(true) { Owner = this };
            if (window.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void ConfigureCreateMode()
        {
            Title = "Create Time Slot";
            searchPanel.Visibility = Visibility.Collapsed;
            gridTimeSlots.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(0);
            btnAdd.Content = "Create";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnExport.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            Width = 600;
            Height = 520;
            MinWidth = 600;
            MinHeight = 520;
            ClearEditor();
        }

        private static DataTable CreateTableSchema()
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(long));
            table.Columns.Add("Code");
            table.Columns.Add("Name");
            table.Columns.Add("Start");
            table.Columns.Add("End");
            table.Columns.Add("Sort");
            table.Columns.Add("Bell");
            return table;
        }

        private void LoadData(long? preferredId = null)
        {
            _table = CreateTableSchema();

            foreach (var slot in _service.GetAll().OrderBy(s => s.SortOrder).ThenBy(s => s.StartTime))
            {
                _table.Rows.Add(
                    slot.Id,
                    slot.Code,
                    slot.Name,
                    slot.StartTime.ToString(@"hh\:mm"),
                    slot.EndTime.ToString(@"hh\:mm"),
                    slot.SortOrder,
                    slot.IsBellPeriod ? "Yes" : "No");
            }

            gridTimeSlots.ItemsSource = _table.DefaultView;
            ApplyFilter(preferredId);
        }

        private void ApplyFilter(long? preferredId = null)
        {
            var term = (txtSearch.Text ?? string.Empty).Trim().Replace("'", "''");
            _table.DefaultView.RowFilter = string.IsNullOrWhiteSpace(term)
                ? string.Empty
                : $"Code LIKE '%{term}%' OR Name LIKE '%{term}%' OR Bell LIKE '%{term}%'";

            if (preferredId.HasValue && SelectRow(preferredId.Value))
            {
                return;
            }

            _selectedId = null;
            gridTimeSlots.SelectedItem = null;
        }

        private bool SelectRow(long id)
        {
            foreach (var item in gridTimeSlots.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == id)
                {
                    gridTimeSlots.SelectedItem = item;
                    gridTimeSlots.ScrollIntoView(item);
                    _selectedId = id;
                    return true;
                }
            }

            return false;
        }

        private void GridTimeSlots_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridTimeSlots.SelectedItem is not DataRowView row)
            {
                _selectedId = null;
                return;
            }

            _selectedId = row.Row.Field<long>("Id");
            txtCode.Text = row.Row["Code"]?.ToString() ?? string.Empty;
            txtName.Text = row.Row["Name"]?.ToString() ?? string.Empty;
            txtStart.Text = row.Row["Start"]?.ToString() ?? "07:00";
            txtEnd.Text = row.Row["End"]?.ToString() ?? "08:00";
            txtSort.Text = row.Row["Sort"]?.ToString() ?? "0";
            chkBellPeriod.IsChecked = string.Equals(row.Row["Bell"]?.ToString(), "Yes", StringComparison.OrdinalIgnoreCase);
        }

        private void AddTimeSlot()
        {
            if (!TryReadEditor(out var code, out var name, out var startTime, out var endTime, out var sortOrder))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var entity = new TimeSlot
            {
                Code = code,
                Name = name,
                StartTime = startTime,
                EndTime = endTime,
                SortOrder = sortOrder,
                IsBellPeriod = chkBellPeriod.IsChecked == true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _service.Create(entity);
            AuditTrailService.Log("CREATE", "time_slots", entity.Id, null, entity);
            if (_createOnly)
            {
                DialogResult = true;
                Close();
                return;
            }

            LoadData(entity.Id);
        }

        private void SaveTimeSlot()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a time slot first.", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryReadEditor(out var code, out var name, out var startTime, out var endTime, out var sortOrder))
            {
                return;
            }

            var entity = _service.GetById(_selectedId.Value);
            if (entity == null)
            {
                return;
            }

            var oldData = new { entity.Code, entity.Name, entity.StartTime, entity.EndTime, entity.SortOrder, entity.IsBellPeriod };
            entity.Code = code;
            entity.Name = name;
            entity.StartTime = startTime;
            entity.EndTime = endTime;
            entity.SortOrder = sortOrder;
            entity.IsBellPeriod = chkBellPeriod.IsChecked == true;
            entity.UpdatedAt = DateTime.UtcNow;

            _service.Update(entity);
            AuditTrailService.Log("UPDATE", "time_slots", entity.Id, oldData, entity);
            LoadData(entity.Id);
        }

        private void DeleteTimeSlot()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a time slot first.", "Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var entity = _service.GetById(_selectedId.Value);
            if (entity == null)
            {
                return;
            }

            var confirm = MessageBox.Show("Delete selected time slot?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _service.Delete(entity.Id);
            AuditTrailService.Log("DELETE", "time_slots", entity.Id, entity, null);
            LoadData();
            ClearEditor();
        }

        private bool TryReadEditor(out string code, out string name, out TimeSpan startTime, out TimeSpan endTime, out int sortOrder)
        {
            code = txtCode.Text.Trim();
            name = string.IsNullOrWhiteSpace(txtName.Text) ? code : txtName.Text.Trim();
            startTime = default;
            endTime = default;
            sortOrder = 0;

            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Code is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!TimeSpan.TryParse(txtStart.Text.Trim(), out startTime))
            {
                MessageBox.Show("Start time must be in HH:mm format.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!TimeSpan.TryParse(txtEnd.Text.Trim(), out endTime))
            {
                MessageBox.Show("End time must be in HH:mm format.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (startTime >= endTime)
            {
                MessageBox.Show("Start time must be earlier than end time.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!int.TryParse(txtSort.Text.Trim(), out sortOrder) || sortOrder < 0)
            {
                MessageBox.Show("Sort order must be a non-negative integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void ClearEditor()
        {
            _selectedId = null;
            txtCode.Clear();
            txtName.Clear();
            txtStart.Text = "07:00";
            txtEnd.Text = "08:00";
            txtSort.Text = "0";
            chkBellPeriod.IsChecked = true;
            gridTimeSlots.SelectedItem = null;
        }
    }
}

using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class RoomsWindow : Window
    {
        private readonly RoomService _roomService = new();
        private readonly bool _createOnly;
        private DataTable _table = new();
        private long? _selectedId;

        public RoomsWindow(bool createOnly = false)
        {
            _createOnly = createOnly;
            InitializeComponent();

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            gridRooms.AutoGeneratingColumn += (_, e) =>
            {
                if (e.PropertyName == "Id")
                {
                    e.Cancel = true;
                }
            };
            gridRooms.SelectionChanged += GridRooms_SelectionChanged;
            btnRefresh.Click += (_, _) => LoadData();
            btnAdd.Click += (_, _) =>
            {
                if (_createOnly)
                {
                    AddRoom();
                }
                else
                {
                    OpenCreateWindow();
                }
            };
            btnSave.Click += (_, _) => SaveRoom();
            btnDelete.Click += (_, _) => DeleteRoom();
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
            btnExport.Click += (_, _) => CsvExportService.SaveDataTable(_table, "rooms.csv");

            chkActive.IsChecked = true;
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
            var window = new RoomsWindow(true) { Owner = this };
            if (window.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void ConfigureCreateMode()
        {
            Title = "Create Room";
            searchPanel.Visibility = Visibility.Collapsed;
            listPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(4);
            btnAdd.Content = "Create";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnRefresh.Visibility = Visibility.Collapsed;
            btnExport.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            Width = 620;
            Height = 430;
            MinWidth = 620;
            MinHeight = 430;
            ClearEditor();
        }

        private void LoadData(long? preferredId = null)
        {
            _table = new DataTable();
            _table.Columns.Add("Id", typeof(long));
            _table.Columns.Add("Code");
            _table.Columns.Add("Name");
            _table.Columns.Add("Capacity");
            _table.Columns.Add("Active");

            foreach (var room in _roomService.GetAll().OrderBy(r => r.Code))
            {
                _table.Rows.Add(
                    room.Id,
                    room.Code,
                    room.Name,
                    room.Capacity?.ToString() ?? string.Empty,
                    room.IsActive ? "Yes" : "No");
            }

            gridRooms.ItemsSource = _table.DefaultView;
            ApplyFilter(preferredId);
        }

        private void ApplyFilter(long? preferredId = null)
        {
            var term = (txtSearch.Text ?? string.Empty).Trim().Replace("'", "''");
            _table.DefaultView.RowFilter = string.IsNullOrWhiteSpace(term)
                ? string.Empty
                : $"Code LIKE '%{term}%' OR Name LIKE '%{term}%' OR Active LIKE '%{term}%'";

            if (preferredId.HasValue && SelectRow(preferredId.Value))
            {
                return;
            }

            _selectedId = null;
            gridRooms.SelectedItem = null;
        }

        private bool SelectRow(long id)
        {
            foreach (var item in gridRooms.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == id)
                {
                    gridRooms.SelectedItem = item;
                    gridRooms.ScrollIntoView(item);
                    _selectedId = id;
                    return true;
                }
            }

            return false;
        }

        private void GridRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridRooms.SelectedItem is not DataRowView row)
            {
                _selectedId = null;
                return;
            }

            _selectedId = row.Row.Field<long>("Id");
            txtCode.Text = row.Row["Code"]?.ToString() ?? string.Empty;
            txtName.Text = row.Row["Name"]?.ToString() ?? string.Empty;
            txtCapacity.Text = row.Row["Capacity"]?.ToString() ?? string.Empty;
            chkActive.IsChecked = string.Equals(row.Row["Active"]?.ToString(), "Yes", StringComparison.OrdinalIgnoreCase);
        }

        private void AddRoom()
        {
            if (!TryReadEditor(out var code, out var name, out var capacity))
            {
                return;
            }

            var now = DateTime.UtcNow;
            var entity = new Room
            {
                Code = code,
                Name = name,
                Capacity = capacity,
                IsActive = chkActive.IsChecked == true,
                CreatedAt = now,
                UpdatedAt = now
            };

            _roomService.Create(entity);
            AuditTrailService.Log("CREATE", "rooms", entity.Id, null, entity);
            if (_createOnly)
            {
                DialogResult = true;
                Close();
                return;
            }

            LoadData(entity.Id);
        }

        private void SaveRoom()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a room first.", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryReadEditor(out var code, out var name, out var capacity))
            {
                return;
            }

            var entity = _roomService.GetById(_selectedId.Value);
            if (entity == null)
            {
                return;
            }

            var oldData = new { entity.Code, entity.Name, entity.Capacity, entity.IsActive };
            entity.Code = code;
            entity.Name = name;
            entity.Capacity = capacity;
            entity.IsActive = chkActive.IsChecked == true;
            entity.UpdatedAt = DateTime.UtcNow;

            _roomService.Update(entity);
            AuditTrailService.Log("UPDATE", "rooms", entity.Id, oldData, entity);
            LoadData(entity.Id);
        }

        private void DeleteRoom()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a room first.", "Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var room = _roomService.GetById(_selectedId.Value);
            if (room == null)
            {
                return;
            }

            var confirm = MessageBox.Show("Delete selected room?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            _roomService.Delete(room.Id);
            AuditTrailService.Log("DELETE", "rooms", room.Id, room, null);
            LoadData();
            ClearEditor();
        }

        private bool TryReadEditor(out string code, out string name, out int? capacity)
        {
            code = txtCode.Text.Trim();
            name = txtName.Text.Trim();
            capacity = null;

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Code and Name are required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var capacityText = txtCapacity.Text.Trim();
            if (!string.IsNullOrWhiteSpace(capacityText))
            {
                if (!int.TryParse(capacityText, out var parsed) || parsed < 0)
                {
                    MessageBox.Show("Capacity must be a non-negative integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                capacity = parsed == 0 ? null : parsed;
            }

            return true;
        }

        private void ClearEditor()
        {
            _selectedId = null;
            txtCode.Clear();
            txtName.Clear();
            txtCapacity.Clear();
            chkActive.IsChecked = true;
            gridRooms.SelectedItem = null;
        }
    }
}

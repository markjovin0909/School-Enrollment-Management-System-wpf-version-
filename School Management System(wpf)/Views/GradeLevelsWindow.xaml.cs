using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class GradeLevelsWindow : Window
    {
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly bool _createOnly;
        private DataTable _table = new();
        private long? _selectedId;

        public GradeLevelsWindow(bool createOnly = false)
        {
            _createOnly = createOnly;
            InitializeComponent();

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            gridGradeLevels.AutoGeneratingColumn += (_, e) =>
            {
                if (e.PropertyName == "Id")
                {
                    e.Cancel = true;
                }
            };
            gridGradeLevels.SelectionChanged += GridGradeLevels_SelectionChanged;
            btnNew.Click += (_, _) => OpenCreateWindow();
            btnRefresh.Click += (_, _) => LoadData();
            btnAdd.Click += (_, _) =>
            {
                if (_createOnly)
                {
                    AddGradeLevel();
                }
                else
                {
                    OpenCreateWindow();
                }
            };
            btnSave.Click += (_, _) => SaveGradeLevel();
            btnDelete.Click += (_, _) => DeleteGradeLevel();
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
            var window = new GradeLevelsWindow(true) { Owner = this };
            if (window.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void ConfigureCreateMode()
        {
            Title = "Create Grade Level";
            searchPanel.Visibility = Visibility.Collapsed;
            gridGradeLevels.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(0);
            btnAdd.Content = "Create";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            Width = 560;
            Height = 380;
            MinWidth = 560;
            MinHeight = 380;
            ClearEditor();
        }

        private void LoadData(long? preferredId = null)
        {
            _table = new DataTable();
            _table.Columns.Add("Id", typeof(long));
            _table.Columns.Add("Code");
            _table.Columns.Add("Name");

            foreach (var grade in _gradeLevelService.GetAll())
            {
                _table.Rows.Add(grade.Id, grade.Code, grade.Name);
            }

            gridGradeLevels.ItemsSource = _table.DefaultView;
            ApplyFilter(preferredId);
        }

        private void ApplyFilter(long? preferredId = null)
        {
            var term = (txtSearch.Text ?? string.Empty).Trim().Replace("'", "''");
            _table.DefaultView.RowFilter = string.IsNullOrWhiteSpace(term)
                ? string.Empty
                : $"Code LIKE '%{term}%' OR Name LIKE '%{term}%'";

            if (preferredId.HasValue && SelectRow(preferredId.Value))
            {
                return;
            }

            _selectedId = null;
            gridGradeLevels.SelectedItem = null;
        }

        private bool SelectRow(long id)
        {
            foreach (var item in gridGradeLevels.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == id)
                {
                    gridGradeLevels.SelectedItem = item;
                    gridGradeLevels.ScrollIntoView(item);
                    _selectedId = id;
                    return true;
                }
            }

            return false;
        }

        private void GridGradeLevels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridGradeLevels.SelectedItem is not DataRowView row)
            {
                _selectedId = null;
                return;
            }

            _selectedId = row.Row.Field<long>("Id");
            txtCode.Text = row.Row["Code"]?.ToString() ?? string.Empty;
            txtName.Text = row.Row["Name"]?.ToString() ?? string.Empty;
        }

        private void AddGradeLevel()
        {
            var entity = new GradeLevel
            {
                Code = txtCode.Text.Trim(),
                Name = txtName.Text.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _gradeLevelService.Create(entity);
                AuditTrailService.Log("CREATE", "grade_levels", entity.Id, null, entity);
                if (_createOnly)
                {
                    DialogResult = true;
                    Close();
                    return;
                }

                LoadData(entity.Id);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Grade Level", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create grade level: {ex.Message}", "Grade Level", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveGradeLevel()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a grade level first.", "Grade Level", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var grade = _gradeLevelService.GetById(_selectedId.Value);
            if (grade == null)
            {
                return;
            }

            var oldData = new { grade.Code, grade.Name };
            grade.Code = txtCode.Text.Trim();
            grade.Name = txtName.Text.Trim();

            try
            {
                _gradeLevelService.Update(grade);
                AuditTrailService.Log("UPDATE", "grade_levels", grade.Id, oldData, grade);
                LoadData(grade.Id);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Grade Level", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update grade level: {ex.Message}", "Grade Level", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteGradeLevel()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a grade level first.", "Grade Level", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show("Delete selected grade level?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var grade = _gradeLevelService.GetById(_selectedId.Value);
            if (grade == null)
            {
                return;
            }

            try
            {
                AuditTrailService.Log("DELETE", "grade_levels", grade.Id, grade, null);
                _gradeLevelService.Delete(grade.Id);
                LoadData();
                ClearEditor();
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Grade Level", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete grade level: {ex.Message}", "Grade Level", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearEditor()
        {
            _selectedId = null;
            txtCode.Clear();
            txtName.Clear();
            gridGradeLevels.SelectedItem = null;
        }
    }
}

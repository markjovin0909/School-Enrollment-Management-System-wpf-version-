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
        private readonly EditorMode _mode;
        private DataTable _table = new();
        private long? _selectedId;
        private long? _editId;

        private enum EditorMode
        {
            ListEmbedded,
            Create,
            Edit
        }

        public GradeLevelsWindow()
            : this(EditorMode.ListEmbedded, null)
        {
        }

        public GradeLevelsWindow(bool createOnly)
            : this(createOnly ? EditorMode.Create : EditorMode.ListEmbedded, null)
        {
        }

        public GradeLevelsWindow(long editId)
            : this(EditorMode.Edit, editId)
        {
        }

        private GradeLevelsWindow(EditorMode mode, long? editId)
        {
            _mode = mode;
            _editId = editId;
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
            gridGradeLevels.MouseDoubleClick += (_, _) => OpenEditWindow();
            btnNew.Click += (_, _) => OpenCreateWindow();
            btnListEdit.Click += (_, _) => OpenEditWindow();
            btnListDelete.Click += (_, _) => DeleteGradeLevel();
            btnRefresh.Click += (_, _) => LoadData();
            btnAdd.Click += (_, _) =>
            {
                if (_mode == EditorMode.Create)
                {
                    AddGradeLevel();
                }
                else if (_mode == EditorMode.Edit)
                {
                    SaveGradeLevel();
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
                if (_mode != EditorMode.ListEmbedded)
                {
                    Close();
                }
                else
                {
                    ClearEditor();
                }
            };

            if (_mode == EditorMode.Create)
            {
                ConfigureCreateMode();
            }
            else if (_mode == EditorMode.Edit)
            {
                ConfigureEditMode();
            }
            else
            {
                ConfigureListMode();
                LoadData();
            }
        }

        private void ConfigureListMode()
        {
            editorPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(searchPanel, 0);
            Grid.SetColumnSpan(searchPanel, 2);
            searchPanel.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetColumnSpan(listPanel, 2);
            listPanel.Margin = new Thickness(0);
        }

        private void OpenCreateWindow()
        {
            var window = new GradeLevelsWindow(true);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, listPanel, searchPanel) == true)
            {
                LoadData();
            }
        }

        private void OpenEditWindow()
        {
            if (_mode != EditorMode.ListEmbedded || !_selectedId.HasValue)
            {
                if (_mode == EditorMode.ListEmbedded)
                {
                    MessageBox.Show("Select a grade level first.", "Grade Level", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            var editId = _selectedId.Value;
            var window = new GradeLevelsWindow(editId);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, listPanel, searchPanel) == true)
            {
                LoadData(editId);
            }
        }

        private void ConfigureModalEditorChrome()
        {
            searchPanel.Visibility = Visibility.Collapsed;
            gridGradeLevels.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(0);
            Width = 560;
            Height = 380;
            MinWidth = 560;
            MinHeight = 380;
        }

        private void ConfigureCreateMode()
        {
            Title = "Create Grade Level";
            ConfigureModalEditorChrome();
            btnAdd.Content = "Create";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            ClearEditor();
        }

        private void ConfigureEditMode()
        {
            Title = "Edit Grade Level";
            ConfigureModalEditorChrome();
            btnAdd.Content = "Save";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            LoadEditorFromEntity();
        }

        private void LoadEditorFromEntity()
        {
            if (!_editId.HasValue)
            {
                return;
            }

            var grade = _gradeLevelService.GetById(_editId.Value);
            if (grade == null)
            {
                MessageBox.Show("Grade level not found.", "Grade Level", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            _selectedId = grade.Id;
            txtCode.Text = grade.Code ?? string.Empty;
            txtName.Text = grade.Name ?? string.Empty;
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
                if (_mode == EditorMode.Create)
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
                if (_mode == EditorMode.Edit)
                {
                    DialogResult = true;
                    Close();
                    return;
                }

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

            if (!AppFeedbackService.Confirm("Delete selected grade level?", "Confirm", this))
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

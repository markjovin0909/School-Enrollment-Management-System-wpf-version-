using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class CurriculumWindow : Window
    {
        private readonly CurriculumService _curriculumService = new();
        private readonly CurriculumSubjectService _curriculumSubjectService = new();
        private readonly SubjectService _subjectService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly EditorMode _mode;
        private long? _editId;

        private enum EditorMode
        {
            ListEmbedded,
            Create,
            Edit
        }

        private DataTable _table = new();
        private DataTable _mappingTable = new();
        private long? _selectedCurriculumId;
        private long? _selectedMappingId;
        private bool _modalDirty;
        private List<GradeLevel> _gradeLevels = new();
        private List<Subject> _subjects = new();

        public CurriculumWindow()
            : this(EditorMode.ListEmbedded, null)
        {
        }

        public CurriculumWindow(bool createOnly)
            : this(createOnly ? EditorMode.Create : EditorMode.ListEmbedded, null)
        {
        }

        public CurriculumWindow(long editId)
            : this(EditorMode.Edit, editId)
        {
        }

        private CurriculumWindow(EditorMode mode, long? editId)
        {
            _mode = mode;
            _editId = editId;
            InitializeComponent();

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            gridCurricula.AutoGeneratingColumn += (_, e) =>
            {
                if (e.PropertyName == "Id")
                {
                    e.Cancel = true;
                }
            };
            gridMappings.AutoGeneratingColumn += (_, e) =>
            {
                if (e.PropertyName == "Id")
                {
                    e.Cancel = true;
                }
            };
            gridCurricula.SelectionChanged += GridCurricula_SelectionChanged;
            gridCurricula.MouseDoubleClick += (_, _) => OpenEditWindow();
            gridMappings.SelectionChanged += GridMappings_SelectionChanged;
            gridMappings.MouseDoubleClick += (_, _) => OpenEditMappingWindow();

            btnNew.Click += (_, _) => OpenCreateWindow();
            btnListEdit.Click += (_, _) => OpenEditWindow();
            btnListDelete.Click += (_, _) => DeleteCurriculum();
            btnRefresh.Click += (_, _) => LoadData();
            btnAdd.Click += (_, _) =>
            {
                if (_mode == EditorMode.Create)
                {
                    AddCurriculum();
                }
                else if (_mode == EditorMode.Edit)
                {
                    SaveCurriculum();
                }
                else
                {
                    OpenCreateWindow();
                }
            };
            btnSave.Click += (_, _) => SaveCurriculum();
            btnDelete.Click += (_, _) => DeleteCurriculum();
            btnClear.Click += (_, _) =>
            {
                if (_mode != EditorMode.ListEmbedded)
                {
                    if (_modalDirty)
                    {
                        DialogResult = true;
                    }

                    Close();
                }
                else
                {
                    ClearEditor();
                }
            };

            btnMapAdd.Click += (_, _) => OpenAddMappingWindow();
            btnMapEdit.Click += (_, _) => OpenEditMappingWindow();
            btnMapDelete.Click += (_, _) => RemoveMapping();

            cboMapSemester.ItemsSource = new[] { string.Empty, "1", "2" };
            cboMapRequired.ItemsSource = new[] { "Yes", "No" };
            cboMapSemester.SelectedIndex = 0;
            cboMapRequired.SelectedIndex = 0;
            txtMapSort.Text = "0";
            chkActive.IsChecked = true;

            LoadLookups();
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
            mappingPanel.Visibility = Visibility.Collapsed;
            mappedGridPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(searchPanel, 0);
            Grid.SetColumnSpan(searchPanel, 2);
            searchPanel.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetColumnSpan(listPanel, 2);
            listPanel.Margin = new Thickness(0);
        }

        private void OpenCreateWindow()
        {
            var window = new CurriculumWindow(true);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, listPanel, searchPanel, mappingPanel, mappedGridPanel) == true)
            {
                LoadData();
            }
        }

        private void OpenEditWindow()
        {
            if (_mode != EditorMode.ListEmbedded || !_selectedCurriculumId.HasValue)
            {
                if (_mode == EditorMode.ListEmbedded)
                {
                    MessageBox.Show("Select a curriculum first.", "Curriculum", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            var editId = _selectedCurriculumId.Value;
            var window = new CurriculumWindow(editId);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, listPanel, searchPanel, mappingPanel, mappedGridPanel) == true)
            {
                LoadData(editId);
            }
        }

        private void ConfigureCreateMode()
        {
            Title = "Create Curriculum";
            searchPanel.Visibility = Visibility.Collapsed;
            gridCurricula.Visibility = Visibility.Collapsed;
            listPanel.Visibility = Visibility.Collapsed;
            mappingPanel.Visibility = Visibility.Collapsed;
            mappedGridPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(0);
            btnAdd.Content = "Create";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            Width = 680;
            Height = 460;
            MinWidth = 680;
            MinHeight = 460;
            ClearEditor();
        }

        private void ConfigureEditMode()
        {
            Title = "Edit Curriculum";
            searchPanel.Visibility = Visibility.Collapsed;
            gridCurricula.Visibility = Visibility.Collapsed;
            listPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetColumn(mappingPanel, 0);
            Grid.SetColumnSpan(mappingPanel, 2);
            mappingPanel.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetColumn(mappedGridPanel, 0);
            Grid.SetColumnSpan(mappedGridPanel, 2);
            mappedGridPanel.Margin = new Thickness(0);
            btnAdd.Content = "Save";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            Width = 760;
            Height = 720;
            MinWidth = 760;
            MinHeight = 640;
            LoadEditorFromEntity();
        }

        private void LoadEditorFromEntity()
        {
            if (!_editId.HasValue)
            {
                return;
            }

            var curriculum = _curriculumService.GetById(_editId.Value);
            if (curriculum == null)
            {
                MessageBox.Show("Curriculum not found.", "Curriculum", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            _selectedCurriculumId = curriculum.Id;
            txtName.Text = curriculum.Name;
            txtDescription.Text = curriculum.Description ?? string.Empty;
            chkActive.IsChecked = curriculum.IsActive;
            LoadMappings();
        }

        private void LoadLookups()
        {
            _gradeLevels = _gradeLevelService.GetAll().OrderBy(g => g.Code).ThenBy(g => g.Name).ToList();
            _subjects = _subjectService.GetAll().OrderBy(s => s.Code).ThenBy(s => s.Title).ToList();
            ClearMappingSummary();
        }

        private void LoadData(long? preferredCurriculumId = null)
        {
            _table = new DataTable();
            _table.Columns.Add("Id", typeof(long));
            _table.Columns.Add("Name");
            _table.Columns.Add("Active");

            foreach (var curriculum in _curriculumService.GetAll())
            {
                _table.Rows.Add(curriculum.Id, curriculum.Name, curriculum.IsActive ? "Yes" : "No");
            }

            gridCurricula.ItemsSource = _table.DefaultView;
            ApplyFilter(preferredCurriculumId);
        }

        private void ApplyFilter(long? preferredCurriculumId = null)
        {
            var term = (txtSearch.Text ?? string.Empty).Trim().Replace("'", "''");
            _table.DefaultView.RowFilter = string.IsNullOrWhiteSpace(term)
                ? string.Empty
                : $"Name LIKE '%{term}%' OR Active LIKE '%{term}%'";

            if (preferredCurriculumId.HasValue && SelectCurriculum(preferredCurriculumId.Value))
            {
                return;
            }

            _selectedCurriculumId = null;
            gridCurricula.SelectedItem = null;
            LoadMappings();
        }

        private bool SelectCurriculum(long id)
        {
            foreach (var item in gridCurricula.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == id)
                {
                    gridCurricula.SelectedItem = item;
                    gridCurricula.ScrollIntoView(item);
                    _selectedCurriculumId = id;
                    return true;
                }
            }

            return false;
        }

        private void GridCurricula_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridCurricula.SelectedItem is not DataRowView row)
            {
                _selectedCurriculumId = null;
                LoadMappings();
                return;
            }

            _selectedCurriculumId = row.Row.Field<long>("Id");
            var curriculum = _curriculumService.GetById(_selectedCurriculumId.Value);
            if (curriculum == null)
            {
                return;
            }

            txtName.Text = curriculum.Name;
            txtDescription.Text = curriculum.Description ?? string.Empty;
            chkActive.IsChecked = curriculum.IsActive;
            LoadMappings();
        }

        private void LoadMappings(long? preferredMappingId = null)
        {
            _mappingTable = new DataTable();
            _mappingTable.Columns.Add("Id", typeof(long));
            _mappingTable.Columns.Add("Grade");
            _mappingTable.Columns.Add("Subject");
            _mappingTable.Columns.Add("Semester");
            _mappingTable.Columns.Add("Required");
            _mappingTable.Columns.Add("Sort");

            if (_selectedCurriculumId.HasValue)
            {
                var mappings = _curriculumSubjectService.GetAll()
                    .Where(x => x.CurriculumId == _selectedCurriculumId.Value)
                    .OrderBy(x => x.GradeLevelId)
                    .ThenBy(x => x.SortOrder)
                    .ThenBy(x => x.Id)
                    .ToList();

                foreach (var mapping in mappings)
                {
                    var grade = _gradeLevels.FirstOrDefault(g => g.Id == mapping.GradeLevelId);
                    var subject = _subjects.FirstOrDefault(s => s.Id == mapping.SubjectId);
                    var gradeLabel = grade == null ? $"Grade {mapping.GradeLevelId}" : (string.IsNullOrWhiteSpace(grade.Code) ? grade.Name : grade.Code);
                    var subjectLabel = subject == null
                        ? $"Subject {mapping.SubjectId}"
                        : (string.IsNullOrWhiteSpace(subject.Code) ? subject.Title : $"{subject.Code} - {subject.Title}");

                    _mappingTable.Rows.Add(
                        mapping.Id,
                        gradeLabel,
                        subjectLabel,
                        mapping.Semester?.ToString() ?? string.Empty,
                        mapping.IsRequired ? "Yes" : "No",
                        mapping.SortOrder);
                }
            }

            gridMappings.ItemsSource = _mappingTable.DefaultView;

            if (preferredMappingId.HasValue)
            {
                foreach (var item in gridMappings.Items)
                {
                    if (item is DataRowView row && row.Row.Field<long>("Id") == preferredMappingId.Value)
                    {
                        gridMappings.SelectedItem = item;
                        gridMappings.ScrollIntoView(item);
                        return;
                    }
                }
            }

            _selectedMappingId = null;
            gridMappings.SelectedItem = null;
        }

        private void GridMappings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridMappings.SelectedItem is not DataRowView row)
            {
                _selectedMappingId = null;
                ClearMappingSummary();
                return;
            }

            _selectedMappingId = row.Row.Field<long>("Id");
            var mapping = _curriculumSubjectService.GetById(_selectedMappingId.Value);
            if (mapping == null)
            {
                ClearMappingSummary();
                return;
            }

            var subjectItems = _subjects
                .Where(s => !s.GradeLevelId.HasValue || s.GradeLevelId == mapping.GradeLevelId)
                .OrderBy(s => s.Code)
                .ThenBy(s => s.Title)
                .Select(s => new SubjectOption
                {
                    Id = s.Id,
                    Label = string.IsNullOrWhiteSpace(s.Code) ? s.Title : $"{s.Code} - {s.Title}"
                })
                .ToList();

            cboMapGrade.ItemsSource = _gradeLevels;
            cboMapGrade.SelectedValue = mapping.GradeLevelId;
            cboMapSubject.ItemsSource = subjectItems;
            cboMapSubject.SelectedValue = mapping.SubjectId;
            cboMapSemester.SelectedItem = mapping.Semester?.ToString() ?? string.Empty;
            cboMapRequired.SelectedItem = mapping.IsRequired ? "Yes" : "No";
            txtMapSort.Text = mapping.SortOrder.ToString();
        }

        private void OpenAddMappingWindow()
        {
            if (!_selectedCurriculumId.HasValue)
            {
                MessageBox.Show("Select a curriculum first.", "Mapping", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new CurriculumMappingWindow(_selectedCurriculumId.Value);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, mappingPanel, mappedGridPanel, listPanel) == true && window.SavedMappingId.HasValue)
            {
                _modalDirty = true;
                LoadMappings(window.SavedMappingId.Value);
            }
        }

        private void OpenEditMappingWindow()
        {
            if (!_selectedCurriculumId.HasValue)
            {
                MessageBox.Show("Select a curriculum first.", "Mapping", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_selectedMappingId.HasValue)
            {
                MessageBox.Show("Select a mapping first.", "Mapping", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new CurriculumMappingWindow(_selectedCurriculumId.Value, _selectedMappingId.Value);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, mappingPanel, mappedGridPanel, listPanel) == true && window.SavedMappingId.HasValue)
            {
                _modalDirty = true;
                LoadMappings(window.SavedMappingId.Value);
            }
        }

        private void AddCurriculum()
        {
            var entity = new Curriculum
            {
                Name = txtName.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim(),
                IsActive = chkActive.IsChecked == true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _curriculumService.Create(entity);
                AuditTrailService.Log("CREATE", "curricula", entity.Id, null, entity);
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
                MessageBox.Show(ex.Message, "Curriculum", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveCurriculum()
        {
            if (!_selectedCurriculumId.HasValue)
            {
                MessageBox.Show("Select a curriculum first.", "Curriculum", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var curriculum = _curriculumService.GetById(_selectedCurriculumId.Value);
            if (curriculum == null)
            {
                return;
            }

            var oldData = new { curriculum.Name, curriculum.Description, curriculum.IsActive };
            curriculum.Name = txtName.Text.Trim();
            curriculum.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
            curriculum.IsActive = chkActive.IsChecked == true;

            try
            {
                _curriculumService.Update(curriculum);
                AuditTrailService.Log("UPDATE", "curricula", curriculum.Id, oldData, curriculum);
                if (_mode == EditorMode.Edit)
                {
                    _modalDirty = true;
                    AppFeedbackService.ShowSuccess("Curriculum updated.", "Curriculum", this);
                    return;
                }

                LoadData(curriculum.Id);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Curriculum", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteCurriculum()
        {
            if (!_selectedCurriculumId.HasValue)
            {
                MessageBox.Show("Select a curriculum first.", "Curriculum", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!AppFeedbackService.Confirm("Delete selected curriculum?", "Confirm", this))
            {
                return;
            }

            var curriculum = _curriculumService.GetById(_selectedCurriculumId.Value);
            if (curriculum == null)
            {
                return;
            }

            try
            {
                AuditTrailService.Log("DELETE", "curricula", curriculum.Id, curriculum, null);
                _curriculumService.Delete(curriculum.Id);
                LoadData();
                ClearEditor();
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Curriculum", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveMapping()
        {
            if (!_selectedMappingId.HasValue)
            {
                MessageBox.Show("Select a mapping first.", "Mapping", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mapping = _curriculumSubjectService.GetById(_selectedMappingId.Value);
            if (mapping == null)
            {
                return;
            }

            if (!AppFeedbackService.Confirm("Remove selected mapping?", "Confirm", this))
            {
                return;
            }

            AuditTrailService.Log("DELETE", "curriculum_subjects", mapping.Id, mapping, null);
            _curriculumSubjectService.Delete(mapping.Id);
            _modalDirty = true;
            LoadMappings();
        }

        private void ClearEditor()
        {
            _selectedCurriculumId = null;
            _selectedMappingId = null;
            txtName.Clear();
            txtDescription.Clear();
            chkActive.IsChecked = true;
            gridCurricula.SelectedItem = null;
            LoadMappings();
            ClearMappingSummary();
        }

        private void ClearMappingSummary()
        {
            cboMapGrade.ItemsSource = _gradeLevels;
            cboMapGrade.SelectedIndex = _gradeLevels.Count > 0 ? 0 : -1;
            cboMapSubject.ItemsSource = Array.Empty<SubjectOption>();
            cboMapSemester.SelectedIndex = 0;
            cboMapRequired.SelectedIndex = 0;
            txtMapSort.Text = "0";
        }

        private sealed class SubjectOption
        {
            public long Id { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}

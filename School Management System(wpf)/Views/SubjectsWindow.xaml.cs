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
    public partial class SubjectsWindow : Window
    {
        private readonly SubjectService _subjectService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly EditorMode _mode;
        private long? _editId;

        private List<GradeLevel> _gradeLevels = new();
        private DataTable _table = new();
        private long? _selectedId;

        private enum EditorMode
        {
            ListEmbedded,
            Create,
            Edit
        }

        public SubjectsWindow()
            : this(EditorMode.ListEmbedded, null)
        {
        }

        public SubjectsWindow(bool createOnly)
            : this(createOnly ? EditorMode.Create : EditorMode.ListEmbedded, null)
        {
        }

        public SubjectsWindow(long editId)
            : this(EditorMode.Edit, editId)
        {
        }

        private SubjectsWindow(EditorMode mode, long? editId)
        {
            _mode = mode;
            _editId = editId;
            InitializeComponent();

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            gridSubjects.AutoGeneratingColumn += (_, e) =>
            {
                if (e.PropertyName == "Id")
                {
                    e.Cancel = true;
                }
            };
            gridSubjects.SelectionChanged += GridSubjects_SelectionChanged;
            gridSubjects.MouseDoubleClick += (_, _) => OpenEditWindow();
            btnNew.Click += (_, _) => OpenCreateWindow();
            btnListEdit.Click += (_, _) => OpenEditWindow();
            btnListDelete.Click += (_, _) => ArchiveSubject();
            btnRefresh.Click += (_, _) => LoadData();
            btnAdd.Click += (_, _) =>
            {
                if (_mode == EditorMode.Create)
                {
                    AddSubject();
                }
                else if (_mode == EditorMode.Edit)
                {
                    SaveSubject();
                }
                else
                {
                    OpenCreateWindow();
                }
            };
            btnSave.Click += (_, _) => SaveSubject();
            btnDelete.Click += (_, _) => ArchiveSubject();
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

            LoadLookups();
            chkActive.IsChecked = true;
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
            Grid.SetColumnSpan(listPanel, 2);
            listPanel.Margin = new Thickness(0);
        }

        private void OpenCreateWindow()
        {
            var window = new SubjectsWindow(true);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, listPanel, searchPanel) == true)
            {
                LoadLookups();
                LoadData();
            }
        }

        private void OpenEditWindow()
        {
            if (_mode != EditorMode.ListEmbedded || !_selectedId.HasValue)
            {
                if (_mode == EditorMode.ListEmbedded)
                {
                    MessageBox.Show("Select a subject first.", "Subject", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            var editId = _selectedId.Value;
            var window = new SubjectsWindow(editId);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, listPanel, searchPanel) == true)
            {
                LoadLookups();
                LoadData(editId);
            }
        }

        private void ConfigureCreateMode()
        {
            Title = "Create Subject";
            searchPanel.Visibility = Visibility.Collapsed;
            listPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(4);
            btnAdd.Content = "Create";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnRefresh.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            Width = 700;
            Height = 520;
            MinWidth = 700;
            MinHeight = 520;
            ClearEditor();
        }

        private void ConfigureEditMode()
        {
            Title = "Edit Subject";
            searchPanel.Visibility = Visibility.Collapsed;
            listPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(4);
            btnAdd.Content = "Save";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnRefresh.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            Width = 700;
            Height = 520;
            MinWidth = 700;
            MinHeight = 520;
            LoadEditorFromEntity();
        }

        private void LoadEditorFromEntity()
        {
            if (!_editId.HasValue)
            {
                return;
            }

            var subject = _subjectService.GetById(_editId.Value);
            if (subject == null)
            {
                MessageBox.Show("Subject not found.", "Subject", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            _selectedId = subject.Id;
            txtCode.Text = subject.Code;
            txtTitle.Text = subject.Title;
            txtDescription.Text = subject.Description ?? string.Empty;
            cboGradeLevel.SelectedValue = subject.GradeLevelId ?? 0L;
            if (subject.GradeLevelId == null)
            {
                cboGradeLevel.SelectedIndex = -1;
            }
            chkActive.IsChecked = subject.IsActive;
        }

        private void LoadLookups()
        {
            _gradeLevels = _gradeLevelService.GetAll().ToList();
            cboGradeLevel.ItemsSource = _gradeLevels;
            cboGradeLevel.SelectedIndex = -1;
        }

        private void LoadData(long? preferredId = null)
        {
            var gradeMap = _gradeLevels
                .GroupBy(g => g.Id)
                .ToDictionary(g => g.Key, g => g.First().Code ?? string.Empty);

            _table = new DataTable();
            _table.Columns.Add("Id", typeof(long));
            _table.Columns.Add("Code");
            _table.Columns.Add("Title");
            _table.Columns.Add("GradeLevel");
            _table.Columns.Add("Active");

            foreach (var subject in _subjectService.GetAll())
            {
                var gradeCode = subject.GradeLevelId.HasValue && gradeMap.ContainsKey(subject.GradeLevelId.Value)
                    ? gradeMap[subject.GradeLevelId.Value]
                    : string.Empty;
                _table.Rows.Add(subject.Id, subject.Code, subject.Title, gradeCode, subject.IsActive ? "Yes" : "No");
            }

            gridSubjects.ItemsSource = _table.DefaultView;
            ApplyFilter(preferredId);
        }

        private void ApplyFilter(long? preferredId = null)
        {
            var term = (txtSearch.Text ?? string.Empty).Trim().Replace("'", "''");
            _table.DefaultView.RowFilter = string.IsNullOrWhiteSpace(term)
                ? string.Empty
                : $"Code LIKE '%{term}%' OR Title LIKE '%{term}%' OR GradeLevel LIKE '%{term}%' OR Active LIKE '%{term}%'";

            if (preferredId.HasValue && SelectRow(preferredId.Value))
            {
                return;
            }

            _selectedId = null;
            gridSubjects.SelectedItem = null;
        }

        private bool SelectRow(long id)
        {
            foreach (var item in gridSubjects.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == id)
                {
                    gridSubjects.SelectedItem = item;
                    gridSubjects.ScrollIntoView(item);
                    _selectedId = id;
                    return true;
                }
            }

            return false;
        }

        private void GridSubjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridSubjects.SelectedItem is not DataRowView row)
            {
                _selectedId = null;
                return;
            }

            _selectedId = row.Row.Field<long>("Id");
            var subject = _subjectService.GetById(_selectedId.Value);
            if (subject == null)
            {
                return;
            }

            txtCode.Text = subject.Code;
            txtTitle.Text = subject.Title;
            txtDescription.Text = subject.Description ?? string.Empty;
            cboGradeLevel.SelectedValue = subject.GradeLevelId ?? 0L;
            if (subject.GradeLevelId == null)
            {
                cboGradeLevel.SelectedIndex = -1;
            }
            chkActive.IsChecked = subject.IsActive;
        }

        private void AddSubject()
        {
            var code = txtCode.Text.Trim();
            if (IsDuplicateSubjectCode(code, null))
            {
                MessageBox.Show("Subject code already exists.", "Subject", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var entity = new Subject
            {
                Code = code,
                Title = txtTitle.Text.Trim(),
                Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim(),
                GradeLevelId = cboGradeLevel.SelectedValue is long gradeLevelId ? gradeLevelId : null,
                IsActive = chkActive.IsChecked == true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _subjectService.Create(entity);
                AuditTrailService.Log("CREATE", "subjects", entity.Id, null, entity);
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
                MessageBox.Show(ex.Message, "Subject", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create subject: {ex.Message}", "Subject", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSubject()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a subject first.", "Subject", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var subject = _subjectService.GetById(_selectedId.Value);
            if (subject == null)
            {
                return;
            }

            var code = txtCode.Text.Trim();
            if (IsDuplicateSubjectCode(code, subject.Id))
            {
                MessageBox.Show("Subject code already exists.", "Subject", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var oldData = new
            {
                subject.Code,
                subject.Title,
                subject.Description,
                subject.IsActive,
                subject.GradeLevelId
            };

            subject.Code = code;
            subject.Title = txtTitle.Text.Trim();
            subject.Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim();
            subject.GradeLevelId = cboGradeLevel.SelectedValue is long gradeLevelId ? gradeLevelId : null;
            subject.IsActive = chkActive.IsChecked == true;

            try
            {
                _subjectService.Update(subject);
                AuditTrailService.Log("UPDATE", "subjects", subject.Id, oldData, subject);
                if (_mode == EditorMode.Edit)
                {
                    DialogResult = true;
                    Close();
                    return;
                }

                LoadData(subject.Id);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Subject", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update subject: {ex.Message}", "Subject", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ArchiveSubject()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a subject first.", "Subject", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!AppFeedbackService.Confirm("Archive selected subject?", "Confirm", this))
            {
                return;
            }

            var subject = _subjectService.GetById(_selectedId.Value);
            if (subject == null)
            {
                return;
            }

            try
            {
                AuditTrailService.Log("DELETE", "subjects", subject.Id, subject, null);
                _subjectService.Delete(subject.Id);
                LoadData();
                ClearEditor();
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Subject", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to archive subject: {ex.Message}", "Subject", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearEditor()
        {
            _selectedId = null;
            txtCode.Clear();
            txtTitle.Clear();
            txtDescription.Clear();
            cboGradeLevel.SelectedIndex = -1;
            chkActive.IsChecked = true;
            gridSubjects.SelectedItem = null;
        }

        private bool IsDuplicateSubjectCode(string code, long? excludeId)
        {
            var normalized = (code ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            return _subjectService.GetAll().Any(x =>
                (!excludeId.HasValue || x.Id != excludeId.Value) &&
                string.Equals((x.Code ?? string.Empty).Trim(), normalized, StringComparison.OrdinalIgnoreCase));
        }
    }
}

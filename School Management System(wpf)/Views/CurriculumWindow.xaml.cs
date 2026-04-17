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

        private DataTable _table = new();
        private DataTable _mappingTable = new();
        private long? _selectedCurriculumId;
        private long? _selectedMappingId;
        private List<GradeLevel> _gradeLevels = new();
        private List<Subject> _subjects = new();

        public CurriculumWindow()
        {
            InitializeComponent();

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            cboMapGrade.SelectionChanged += (_, _) => BindMapSubjects();
            gridCurricula.SelectionChanged += GridCurricula_SelectionChanged;
            gridMappings.SelectionChanged += GridMappings_SelectionChanged;

            btnRefresh.Click += (_, _) => LoadData();
            btnAdd.Click += (_, _) => AddCurriculum();
            btnSave.Click += (_, _) => SaveCurriculum();
            btnDelete.Click += (_, _) => DeleteCurriculum();
            btnClear.Click += (_, _) => ClearEditor();

            btnMapAdd.Click += (_, _) => AddMapping();
            btnMapDelete.Click += (_, _) => RemoveMapping();

            cboMapSemester.ItemsSource = new[] { string.Empty, "1", "2" };
            cboMapRequired.ItemsSource = new[] { "Yes", "No" };
            cboMapSemester.SelectedIndex = 0;
            cboMapRequired.SelectedIndex = 0;
            txtMapSort.Text = "0";
            chkActive.IsChecked = true;

            LoadLookups();
            LoadData();
        }

        private void LoadLookups()
        {
            _gradeLevels = _gradeLevelService.GetAll().OrderBy(g => g.Code).ThenBy(g => g.Name).ToList();
            _subjects = _subjectService.GetAll().OrderBy(s => s.Code).ThenBy(s => s.Title).ToList();

            cboMapGrade.ItemsSource = _gradeLevels;
            cboMapGrade.SelectedIndex = _gradeLevels.Count > 0 ? 0 : -1;
            BindMapSubjects();
        }

        private void BindMapSubjects()
        {
            var gradeLevelId = cboMapGrade.SelectedValue is long id ? id : 0;
            var subjectItems = _subjects
                .Where(s => !s.GradeLevelId.HasValue || s.GradeLevelId == gradeLevelId)
                .OrderBy(s => s.Code)
                .ThenBy(s => s.Title)
                .Select(s => new SubjectOption
                {
                    Id = s.Id,
                    Label = string.IsNullOrWhiteSpace(s.Code) ? s.Title : $"{s.Code} - {s.Title}"
                })
                .ToList();

            cboMapSubject.ItemsSource = subjectItems;
            cboMapSubject.SelectedIndex = subjectItems.Count > 0 ? 0 : -1;
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
                return;
            }

            _selectedMappingId = row.Row.Field<long>("Id");
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

            var confirm = MessageBox.Show("Delete selected curriculum?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
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

        private void AddMapping()
        {
            if (!_selectedCurriculumId.HasValue)
            {
                MessageBox.Show("Select a curriculum first.", "Mapping", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cboMapGrade.SelectedValue is not long gradeLevelId || cboMapSubject.SelectedValue is not long subjectId)
            {
                MessageBox.Show("Select grade level and subject.", "Mapping", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtMapSort.Text.Trim(), out var sortOrder) || sortOrder < 0)
            {
                MessageBox.Show("Sort must be a non-negative integer.", "Mapping", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mapping = new CurriculumSubject
            {
                CurriculumId = _selectedCurriculumId.Value,
                GradeLevelId = gradeLevelId,
                SubjectId = subjectId,
                Semester = byte.TryParse(cboMapSemester.SelectedItem?.ToString(), out var semester) ? semester : null,
                IsRequired = string.Equals(cboMapRequired.SelectedItem?.ToString(), "Yes", StringComparison.OrdinalIgnoreCase),
                SortOrder = sortOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _curriculumSubjectService.Create(mapping);
                AuditTrailService.Log("CREATE", "curriculum_subjects", mapping.Id, null, mapping);
                LoadMappings(mapping.Id);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Mapping", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            var confirm = MessageBox.Show("Remove selected mapping?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            AuditTrailService.Log("DELETE", "curriculum_subjects", mapping.Id, mapping, null);
            _curriculumSubjectService.Delete(mapping.Id);
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
        }

        private sealed class SubjectOption
        {
            public long Id { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}

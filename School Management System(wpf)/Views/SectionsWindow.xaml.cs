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
    public partial class SectionsWindow : Window
    {
        private readonly SectionService _sectionService = new();
        private readonly SchoolYearService _schoolYearService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly TeacherService _teacherService = new();

        private DataTable _table = new();
        private long? _selectedId;
        private List<SchoolYear> _schoolYears = new();
        private List<GradeLevel> _gradeLevels = new();
        private List<Teacher> _teachers = new();
        private List<TeacherOption> _teacherOptions = new();
        private bool _suppressEvents;

        public SectionsWindow()
        {
            InitializeComponent();

            txtSearch.TextChanged += (_, _) => { if (!_suppressEvents) LoadData(); };
            cboFilterSchoolYear.SelectionChanged += (_, _) => { if (!_suppressEvents) LoadData(); };
            cboFilterGrade.SelectionChanged += (_, _) => { if (!_suppressEvents) LoadData(); };
            gridSections.SelectionChanged += GridSections_SelectionChanged;

            btnRefresh.Click += (_, _) => LoadData();
            btnAdd.Click += (_, _) => AddSection();
            btnSave.Click += (_, _) => SaveSection();
            btnArchiveRestore.Click += (_, _) => ArchiveOrRestoreSection();
            btnClear.Click += (_, _) => ClearEditor();

            LoadLookups();
            LoadData();
        }

        private void LoadLookups()
        {
            _schoolYears = _schoolYearService.GetAll().ToList();
            _gradeLevels = _gradeLevelService.GetAll().ToList();
            _teachers = _teacherService.GetAll().ToList();

            _teacherOptions = new List<TeacherOption>
            {
                new() { Id = 0, Name = "(None)" }
            };
            _teacherOptions.AddRange(_teachers.Select(t => new TeacherOption
            {
                Id = t.Id,
                Name = $"{t.LastName}, {t.FirstName}"
            }));

            _suppressEvents = true;

            cboSchoolYear.ItemsSource = _schoolYears;
            cboSchoolYear.SelectedIndex = _schoolYears.Count > 0 ? 0 : -1;

            cboGradeLevel.ItemsSource = _gradeLevels;
            cboGradeLevel.SelectedIndex = _gradeLevels.Count > 0 ? 0 : -1;

            cboAdviser.ItemsSource = _teacherOptions;
            cboAdviser.SelectedValue = 0L;

            cboFilterSchoolYear.ItemsSource = _schoolYears;
            cboFilterSchoolYear.SelectedIndex = -1;

            cboFilterGrade.ItemsSource = _gradeLevels;
            cboFilterGrade.SelectedIndex = -1;

            _suppressEvents = false;
        }

        private void LoadData(long? preferredId = null)
        {
            _table = new DataTable();
            _table.Columns.Add("Id", typeof(long));
            _table.Columns.Add("Section");
            _table.Columns.Add("Grade");
            _table.Columns.Add("SchoolYear");
            _table.Columns.Add("Adviser");
            _table.Columns.Add("Capacity");
            _table.Columns.Add("Archived");

            var filterSy = cboFilterSchoolYear.SelectedValue is long syId ? syId : (long?)null;
            var filterGrade = cboFilterGrade.SelectedValue is long gradeId ? gradeId : (long?)null;
            var search = (txtSearch.Text ?? string.Empty).Trim();

            foreach (var section in _sectionService.GetAll())
            {
                if (filterSy.HasValue && section.SchoolYearId != filterSy.Value)
                {
                    continue;
                }

                if (filterGrade.HasValue && section.GradeLevelId != filterGrade.Value)
                {
                    continue;
                }

                var syName = _schoolYears.FirstOrDefault(x => x.Id == section.SchoolYearId)?.Name ?? string.Empty;
                var gradeCode = _gradeLevels.FirstOrDefault(x => x.Id == section.GradeLevelId)?.Code ?? string.Empty;
                var adviser = _teachers.FirstOrDefault(x => x.Id == section.AdviserTeacherId);
                var adviserName = adviser == null ? string.Empty : $"{adviser.LastName}, {adviser.FirstName}";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var match =
                        (section.Name ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        gradeCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        syName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        adviserName.Contains(search, StringComparison.OrdinalIgnoreCase);
                    if (!match)
                    {
                        continue;
                    }
                }

                _table.Rows.Add(
                    section.Id,
                    section.Name,
                    gradeCode,
                    syName,
                    adviserName,
                    section.Capacity?.ToString() ?? string.Empty,
                    section.IsArchived ? "YES" : "NO");
            }

            gridSections.ItemsSource = _table.DefaultView;
            if (preferredId.HasValue)
            {
                SelectRow(preferredId.Value);
            }
            else
            {
                _selectedId = null;
                gridSections.SelectedItem = null;
            }
        }

        private bool SelectRow(long id)
        {
            foreach (var item in gridSections.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == id)
                {
                    gridSections.SelectedItem = item;
                    gridSections.ScrollIntoView(item);
                    _selectedId = id;
                    return true;
                }
            }

            return false;
        }

        private void GridSections_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridSections.SelectedItem is not DataRowView row)
            {
                _selectedId = null;
                return;
            }

            _selectedId = row.Row.Field<long>("Id");
            var section = _sectionService.GetById(_selectedId.Value);
            if (section == null)
            {
                return;
            }

            txtName.Text = section.Name;
            txtCapacity.Text = section.Capacity?.ToString() ?? string.Empty;
            cboSchoolYear.SelectedValue = section.SchoolYearId;
            cboGradeLevel.SelectedValue = section.GradeLevelId;
            cboAdviser.SelectedValue = section.AdviserTeacherId ?? 0L;
        }

        private void AddSection()
        {
            if (!TryReadEditor(out var name, out var schoolYearId, out var gradeLevelId, out var capacity, out var adviserTeacherId))
            {
                return;
            }

            if (IsDuplicateSectionName(name, schoolYearId, gradeLevelId, null))
            {
                MessageBox.Show("Section name already exists for the selected school year and grade level.", "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var section = new Section
            {
                Name = name,
                SchoolYearId = schoolYearId,
                GradeLevelId = gradeLevelId,
                Capacity = capacity,
                AdviserTeacherId = adviserTeacherId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _sectionService.Create(section);
                AuditTrailService.Log("CREATE", "sections", section.Id, null, section);
                LoadData(section.Id);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create section: {ex.Message}", "Section", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSection()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a section first.", "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!TryReadEditor(out var name, out var schoolYearId, out var gradeLevelId, out var capacity, out var adviserTeacherId))
            {
                return;
            }

            var section = _sectionService.GetById(_selectedId.Value);
            if (section == null)
            {
                return;
            }

            if (IsDuplicateSectionName(name, schoolYearId, gradeLevelId, section.Id))
            {
                MessageBox.Show("Section name already exists for the selected school year and grade level.", "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var oldData = new
            {
                section.Name,
                section.Capacity,
                section.SchoolYearId,
                section.GradeLevelId,
                section.AdviserTeacherId
            };

            section.Name = name;
            section.SchoolYearId = schoolYearId;
            section.GradeLevelId = gradeLevelId;
            section.Capacity = capacity;
            section.AdviserTeacherId = adviserTeacherId;
            section.UpdatedAt = DateTime.UtcNow;

            try
            {
                _sectionService.Update(section);
                AuditTrailService.Log("UPDATE", "sections", section.Id, oldData, section);
                LoadData(section.Id);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update section: {ex.Message}", "Section", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ArchiveOrRestoreSection()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a section first.", "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var section = _sectionService.GetById(_selectedId.Value);
            if (section == null)
            {
                return;
            }

            var confirm = MessageBox.Show(
                section.IsArchived
                    ? "Restore selected section?"
                    : "Archive selected section? It will be hidden from enrollment and class offering assignment.",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                if (section.IsArchived)
                {
                    _sectionService.Restore(section.Id);
                    AuditTrailService.Log("RESTORE", "sections", section.Id, new { section.IsArchived }, new { IsArchived = false });
                }
                else
                {
                    AuditTrailService.Log("ARCHIVE", "sections", section.Id, section, new { IsArchived = true });
                    _sectionService.Delete(section.Id);
                }

                LoadData();
                ClearEditor();
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Section operation failed: {ex.Message}", "Section", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool TryReadEditor(out string name, out long schoolYearId, out long gradeLevelId, out int? capacity, out long? adviserTeacherId)
        {
            name = txtName.Text.Trim();
            schoolYearId = 0;
            gradeLevelId = 0;
            capacity = null;
            adviserTeacherId = null;

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Section name is required.", "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cboSchoolYear.SelectedValue is not long syId)
            {
                MessageBox.Show("Select a school year.", "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cboGradeLevel.SelectedValue is not long glId)
            {
                MessageBox.Show("Select a grade level.", "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            schoolYearId = syId;
            gradeLevelId = glId;

            if (!string.IsNullOrWhiteSpace(txtCapacity.Text))
            {
                if (!int.TryParse(txtCapacity.Text.Trim(), out var parsedCapacity) || parsedCapacity <= 0)
                {
                    MessageBox.Show("Capacity must be a positive integer.", "Section", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }

                capacity = parsedCapacity;
            }

            if (cboAdviser.SelectedValue is long adviserId && adviserId > 0)
            {
                adviserTeacherId = adviserId;
            }

            return true;
        }

        private bool IsDuplicateSectionName(string name, long schoolYearId, long gradeLevelId, long? excludeId)
        {
            var normalized = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            return _sectionService.GetAll().Any(x =>
                (!excludeId.HasValue || x.Id != excludeId.Value) &&
                !x.IsArchived &&
                x.SchoolYearId == schoolYearId &&
                x.GradeLevelId == gradeLevelId &&
                string.Equals((x.Name ?? string.Empty).Trim(), normalized, StringComparison.OrdinalIgnoreCase));
        }

        private void ClearEditor()
        {
            _selectedId = null;
            txtName.Clear();
            txtCapacity.Clear();
            cboSchoolYear.SelectedIndex = _schoolYears.Count > 0 ? 0 : -1;
            cboGradeLevel.SelectedIndex = _gradeLevels.Count > 0 ? 0 : -1;
            cboAdviser.SelectedValue = 0L;
            gridSections.SelectedItem = null;
        }

        private sealed class TeacherOption
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}

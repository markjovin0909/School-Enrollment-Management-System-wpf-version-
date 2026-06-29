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
    public partial class ClassOfferingsWindow : Window
    {
        private const int TeacherLoadLimit = 8;

        private readonly ClassOfferingService _offeringService = new();
        private readonly SchoolYearService _schoolYearService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly SectionService _sectionService = new();
        private readonly TeacherService _teacherService = new();
        private readonly SubjectService _subjectService = new();
        private readonly CurriculumService _curriculumService = new();
        private readonly CurriculumSubjectService _curriculumSubjectService = new();

        private DataTable _table = new();
        private long? _selectedId;
        private List<SchoolYear> _schoolYears = new();
        private List<GradeLevel> _gradeLevels = new();
        private List<Section> _sections = new();
        private List<Teacher> _teachers = new();
        private List<Subject> _subjects = new();
        private List<Curriculum> _curricula = new();
        private List<TeacherOption> _teacherOptions = new();
        private bool _suppressEvents;

        public ClassOfferingsWindow()
        {
            InitializeComponent();

            cboSchoolYear.SelectionChanged += FiltersChanged;
            cboGradeLevel.SelectionChanged += FiltersChanged;
            cboSection.SelectionChanged += FiltersChanged;
            txtSearch.TextChanged += (_, _) => { if (!_suppressEvents) LoadOfferings(); };
            gridOfferings.AutoGeneratingColumn += (_, e) =>
            {
                if (e.PropertyName == "Id")
                {
                    e.Cancel = true;
                }
            };
            gridOfferings.SelectionChanged += GridOfferings_SelectionChanged;
            gridOfferings.MouseDoubleClick += (_, _) => OpenEditOfferingWindow();

            btnRefresh.Click += (_, _) => LoadOfferings();
            btnGenerate.Click += (_, _) => GenerateOfferings();
            btnSave.Click += (_, _) => OpenEditOfferingWindow();
            btnFinalize.Click += (_, _) => FinalizeOffering();
            btnDelete.Click += (_, _) => DeleteOffering();
            btnClear.Click += (_, _) => ClearEditor();

            cboStatus.ItemsSource = Enum.GetValues(typeof(ClassOfferingStatus));

            LoadLookups();
            LoadOfferings();
            ClearEditor();
        }

        private void LoadLookups()
        {
            _schoolYears = _schoolYearService.GetAll().Where(x => !x.IsArchived).ToList();
            _gradeLevels = _gradeLevelService.GetAll().ToList();
            _sections = _sectionService.GetAll().ToList();
            _teachers = _teacherService.GetAll().ToList();
            _subjects = _subjectService.GetAll().ToList();
            _curricula = _curriculumService.GetAll().ToList();

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

            cboTeacher.ItemsSource = _teacherOptions;
            cboTeacher.SelectedValue = 0L;

            LoadSectionsCombo();

            _suppressEvents = false;
        }

        private void FiltersChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_suppressEvents)
            {
                return;
            }

            if (sender == cboSchoolYear || sender == cboGradeLevel)
            {
                LoadSectionsCombo();
            }

            LoadOfferings();
        }

        private void LoadSectionsCombo(long? preferredSectionId = null)
        {
            var syId = cboSchoolYear.SelectedValue is long sy ? sy : (long?)null;
            var gradeId = cboGradeLevel.SelectedValue is long gl ? gl : (long?)null;

            var filtered = _sections.Where(s =>
                !s.IsArchived &&
                (!syId.HasValue || s.SchoolYearId == syId.Value) &&
                (!gradeId.HasValue || s.GradeLevelId == gradeId.Value)).ToList();

            cboSection.ItemsSource = filtered;
            if (preferredSectionId.HasValue && filtered.Any(x => x.Id == preferredSectionId.Value))
            {
                cboSection.SelectedValue = preferredSectionId.Value;
            }
            else
            {
                cboSection.SelectedIndex = filtered.Count > 0 ? 0 : -1;
            }
        }

        private void LoadOfferings(long? preferredOfferingId = null)
        {
            _table = new DataTable();
            _table.Columns.Add("Id", typeof(long));
            _table.Columns.Add("School Year");
            _table.Columns.Add("Grade");
            _table.Columns.Add("Curriculum");
            _table.Columns.Add("Subject");
            _table.Columns.Add("Section");
            _table.Columns.Add("Teacher");
            _table.Columns.Add("Room");
            _table.Columns.Add("Status");

            var syId = cboSchoolYear.SelectedValue is long sy ? sy : (long?)null;
            var sectionId = cboSection.SelectedValue is long section ? section : (long?)null;
            var search = (txtSearch.Text ?? string.Empty).Trim();

            foreach (var offering in _offeringService.GetAll())
            {
                if (syId.HasValue && offering.SchoolYearId != syId.Value)
                {
                    continue;
                }

                if (sectionId.HasValue && offering.SectionId != sectionId.Value)
                {
                    continue;
                }

                var subject = _subjects.FirstOrDefault(s => s.Id == offering.SubjectId)?.Title ?? string.Empty;
                var offeringSection = _sections.FirstOrDefault(s => s.Id == offering.SectionId);
                var sectionName = offeringSection?.Name ?? string.Empty;
                var schoolYearName = _schoolYears.FirstOrDefault(s => s.Id == offering.SchoolYearId)?.Name ?? string.Empty;
                var gradeCode = offeringSection == null
                    ? string.Empty
                    : _gradeLevels.FirstOrDefault(g => g.Id == offeringSection.GradeLevelId)?.Code ?? string.Empty;
                var curriculumName = offering.CurriculumId.HasValue
                    ? _curricula.FirstOrDefault(c => c.Id == offering.CurriculumId.Value)?.Name ?? string.Empty
                    : string.Empty;
                var teacher = _teachers.FirstOrDefault(t => t.Id == offering.TeacherId);
                var teacherName = teacher == null ? string.Empty : $"{teacher.LastName}, {teacher.FirstName}";

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var match =
                        schoolYearName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        gradeCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        curriculumName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        subject.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        sectionName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        teacherName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (offering.Room ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        offering.Status.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
                    if (!match)
                    {
                        continue;
                    }
                }

                _table.Rows.Add(
                    offering.Id,
                    schoolYearName,
                    gradeCode,
                    curriculumName,
                    subject,
                    sectionName,
                    teacherName,
                    offering.Room ?? string.Empty,
                    offering.Status.ToString());
            }

            gridOfferings.ItemsSource = _table.DefaultView;

            if (preferredOfferingId.HasValue && SelectOffering(preferredOfferingId.Value))
            {
                return;
            }

            _selectedId = null;
            gridOfferings.SelectedItem = null;
        }

        private bool SelectOffering(long offeringId)
        {
            foreach (var item in gridOfferings.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == offeringId)
                {
                    gridOfferings.SelectedItem = item;
                    gridOfferings.ScrollIntoView(item);
                    _selectedId = offeringId;
                    return true;
                }
            }

            return false;
        }

        private void GridOfferings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridOfferings.SelectedItem is not DataRowView row)
            {
                _selectedId = null;
                return;
            }

            _selectedId = row.Row.Field<long>("Id");
            var offering = _offeringService.GetById(_selectedId.Value);
            if (offering == null)
            {
                return;
            }

            var subject = _subjects.FirstOrDefault(s => s.Id == offering.SubjectId);
            txtSubject.Text = subject?.Title ?? string.Empty;
            txtRoom.Text = offering.Room ?? string.Empty;
            cboTeacher.SelectedValue = offering.TeacherId ?? 0L;
            cboStatus.SelectedItem = offering.Status;
        }

        private void GenerateOfferings()
        {
            if (cboSchoolYear.SelectedValue is not long syId || cboSection.SelectedValue is not long sectionId)
            {
                MessageBox.Show("Select School Year and Section.", "Generate", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var section = _sections.FirstOrDefault(s => s.Id == sectionId);
            if (section == null)
            {
                return;
            }

            var curriculum = _curricula.FirstOrDefault(c => c.IsActive) ?? _curricula.FirstOrDefault();
            if (curriculum == null)
            {
                MessageBox.Show("No curriculum found.", "Generate", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            long? lastCreatedId = null;
            var mappings = _curriculumSubjectService.GetAll()
                .Where(m => m.CurriculumId == curriculum.Id && m.GradeLevelId == section.GradeLevelId)
                .ToList();

            foreach (var mapping in mappings)
            {
                var exists = _offeringService.GetAll().Any(o =>
                    o.SchoolYearId == syId &&
                    o.SectionId == sectionId &&
                    o.SubjectId == mapping.SubjectId);
                if (exists)
                {
                    continue;
                }

                var offering = new ClassOffering
                {
                    SchoolYearId = syId,
                    SectionId = sectionId,
                    SubjectId = mapping.SubjectId,
                    CurriculumId = curriculum.Id,
                    Status = ClassOfferingStatus.DRAFT,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                try
                {
                    _offeringService.Create(offering);
                    AuditTrailService.Log("CREATE", "class_offerings", offering.Id, null, offering);
                    lastCreatedId = offering.Id;
                }
                catch (DomainValidationException ex)
                {
                    MessageBox.Show(ex.Message, "Generate Offerings", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            LoadOfferings(lastCreatedId);
        }

        private void OpenEditOfferingWindow()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select an offering first.", "Edit Offering", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var window = new ClassOfferingEditWindow(_selectedId.Value);
            if (AppFeedbackService.ShowOwnedDialog(window, this, gridOfferings) == true && window.SavedOfferingId.HasValue)
            {
                LoadOfferings(window.SavedOfferingId.Value);
            }
        }

        private void FinalizeOffering()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select an offering first.", "Finalize", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var offering = _offeringService.GetById(_selectedId.Value);
            if (offering == null)
            {
                return;
            }

            if (!AppFeedbackService.Confirm("Finalize selected offering?", "Confirm", this))
            {
                return;
            }

            var oldData = new { offering.Status };
            offering.Status = ClassOfferingStatus.FINALIZED;

            try
            {
                _offeringService.Update(offering);
                AuditTrailService.Log("UPDATE", "class_offerings", offering.Id, oldData, offering);
                LoadOfferings(offering.Id);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "Finalize Offering", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteOffering()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select an offering first.", "Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!AppFeedbackService.Confirm("Delete selected offering?", "Confirm", this))
            {
                return;
            }

            var offering = _offeringService.GetById(_selectedId.Value);
            if (offering != null)
            {
                AuditTrailService.Log("DELETE", "class_offerings", offering.Id, offering, null);
            }

            _offeringService.Delete(_selectedId.Value);
            LoadOfferings();
            ClearEditor();
        }

        private void ClearEditor()
        {
            _selectedId = null;
            txtSubject.Clear();
            txtRoom.Clear();
            cboTeacher.SelectedValue = 0L;
            if (cboStatus.Items.Count > 0)
            {
                cboStatus.SelectedIndex = 0;
            }

            gridOfferings.SelectedItem = null;
        }

        private sealed class TeacherOption
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}

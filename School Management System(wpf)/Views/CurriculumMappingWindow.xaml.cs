using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class CurriculumMappingWindow : Window
    {
        private enum EditorMode
        {
            Create,
            Edit
        }

        private readonly CurriculumService _curriculumService = new();
        private readonly CurriculumSubjectService _curriculumSubjectService = new();
        private readonly SubjectService _subjectService = new();
        private readonly GradeLevelService _gradeLevelService = new();
        private readonly EditorMode _mode;
        private readonly long _curriculumId;
        private readonly long? _mappingId;
        private List<GradeLevel> _gradeLevels = new();
        private List<Subject> _subjects = new();

        public long? SavedMappingId { get; private set; }

        public CurriculumMappingWindow(long curriculumId)
            : this(EditorMode.Create, curriculumId, null)
        {
        }

        public CurriculumMappingWindow(long curriculumId, long mappingId)
            : this(EditorMode.Edit, curriculumId, mappingId)
        {
        }

        private CurriculumMappingWindow(EditorMode mode, long curriculumId, long? mappingId)
        {
            _mode = mode;
            _curriculumId = curriculumId;
            _mappingId = mappingId;

            InitializeComponent();

            cboMapSemester.ItemsSource = new[] { string.Empty, "1", "2" };
            cboMapRequired.ItemsSource = new[] { "Yes", "No" };
            cboMapSemester.SelectedIndex = 0;
            cboMapRequired.SelectedIndex = 0;
            txtMapSort.Text = "0";

            cboMapGrade.SelectionChanged += (_, _) => BindMapSubjects();
            btnSave.Click += (_, _) => SaveMapping();
            btnCancel.Click += (_, _) => Close();

            ConfigureMode();
            LoadLookups();
            LoadExistingValues();
        }

        private void ConfigureMode()
        {
            if (_mode == EditorMode.Edit)
            {
                Title = "Edit Subject Mapping";
                txtDialogTitle.Text = "Edit Subject Mapping";
                txtDialogSubtitle.Text = "Update grade, subject, semester, requirement, and sort order in one modal flow.";
                btnSave.Content = "Save Changes";
                return;
            }

            Title = "Add Subject Mapping";
            txtDialogTitle.Text = "Add Subject Mapping";
            txtDialogSubtitle.Text = "Assign a subject to a curriculum, grade level, and semester in one modal flow.";
            btnSave.Content = "Create Mapping";
        }

        private void LoadLookups()
        {
            var curriculum = _curriculumService.GetById(_curriculumId);
            if (curriculum == null)
            {
                AppFeedbackService.ShowWarning("Curriculum record not found.", "Curriculum Mapping", this);
                Close();
                return;
            }

            txtCurriculumName.Text = curriculum.Name;

            _gradeLevels = _gradeLevelService.GetAll().OrderBy(g => g.Code).ThenBy(g => g.Name).ToList();
            _subjects = _subjectService.GetAll().OrderBy(s => s.Code).ThenBy(s => s.Title).ToList();

            cboMapGrade.ItemsSource = _gradeLevels;
            cboMapGrade.SelectedIndex = _gradeLevels.Count > 0 ? 0 : -1;
        }

        private void BindMapSubjects()
        {
            var gradeLevelId = cboMapGrade.SelectedValue is long id ? id : 0L;
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
            if (cboMapSubject.SelectedValue is long selectedId && subjectItems.Any(x => x.Id == selectedId))
            {
                cboMapSubject.SelectedValue = selectedId;
            }
            else
            {
                cboMapSubject.SelectedIndex = subjectItems.Count > 0 ? 0 : -1;
            }
        }

        private void LoadExistingValues()
        {
            if (_mode != EditorMode.Edit || !_mappingId.HasValue)
            {
                return;
            }

            var mapping = _curriculumSubjectService.GetById(_mappingId.Value);
            if (mapping == null)
            {
                AppFeedbackService.ShowWarning("Mapping record not found.", "Curriculum Mapping", this);
                Close();
                return;
            }

            cboMapGrade.SelectedValue = mapping.GradeLevelId;
            BindMapSubjects();
            cboMapSubject.SelectedValue = mapping.SubjectId;
            cboMapSemester.SelectedItem = mapping.Semester?.ToString() ?? string.Empty;
            cboMapRequired.SelectedItem = mapping.IsRequired ? "Yes" : "No";
            txtMapSort.Text = mapping.SortOrder.ToString();
        }

        private void SaveMapping()
        {
            HideValidationSummary();

            if (cboMapGrade.SelectedValue is not long gradeLevelId || cboMapSubject.SelectedValue is not long subjectId)
            {
                ShowValidationSummary(new[] { "Select grade level and subject." });
                return;
            }

            if (!int.TryParse(txtMapSort.Text.Trim(), out var sortOrder) || sortOrder < 0)
            {
                ShowValidationSummary(new[] { "Sort order must be a non-negative integer." });
                return;
            }

            try
            {
                if (_mode == EditorMode.Edit && _mappingId.HasValue)
                {
                    var mapping = _curriculumSubjectService.GetById(_mappingId.Value);
                    if (mapping == null)
                    {
                        AppFeedbackService.ShowWarning("Mapping record not found.", "Curriculum Mapping", this);
                        return;
                    }

                    var oldData = new { mapping.GradeLevelId, mapping.SubjectId, mapping.Semester, mapping.IsRequired, mapping.SortOrder };
                    mapping.GradeLevelId = gradeLevelId;
                    mapping.SubjectId = subjectId;
                    mapping.Semester = byte.TryParse(cboMapSemester.SelectedItem?.ToString(), out var editSemester) ? editSemester : null;
                    mapping.IsRequired = string.Equals(cboMapRequired.SelectedItem?.ToString(), "Yes", StringComparison.OrdinalIgnoreCase);
                    mapping.SortOrder = sortOrder;
                    mapping.UpdatedAt = DateTime.UtcNow;

                    _curriculumSubjectService.Update(mapping);
                    AuditTrailService.Log("UPDATE", "curriculum_subjects", mapping.Id, oldData, mapping);
                    SavedMappingId = mapping.Id;
                }
                else
                {
                    var mapping = new CurriculumSubject
                    {
                        CurriculumId = _curriculumId,
                        GradeLevelId = gradeLevelId,
                        SubjectId = subjectId,
                        Semester = byte.TryParse(cboMapSemester.SelectedItem?.ToString(), out var createSemester) ? createSemester : null,
                        IsRequired = string.Equals(cboMapRequired.SelectedItem?.ToString(), "Yes", StringComparison.OrdinalIgnoreCase),
                        SortOrder = sortOrder,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _curriculumSubjectService.Create(mapping);
                    AuditTrailService.Log("CREATE", "curriculum_subjects", mapping.Id, null, mapping);
                    SavedMappingId = mapping.Id;
                }

                DialogResult = true;
                Close();
            }
            catch (DomainValidationException ex)
            {
                ShowValidationSummary(new[] { ex.Message });
            }
        }

        private void ShowValidationSummary(IEnumerable<string> errors)
        {
            txtValidationSummary.Text = string.Join(Environment.NewLine, errors.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().Select(x => $"- {x}"));
            validationSummaryHost.Visibility = Visibility.Visible;
        }

        private void HideValidationSummary()
        {
            validationSummaryHost.Visibility = Visibility.Collapsed;
            txtValidationSummary.Text = string.Empty;
        }

        private sealed class SubjectOption
        {
            public long Id { get; set; }
            public string Label { get; set; } = string.Empty;
        }
    }
}

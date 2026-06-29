using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class StudentRequirementsWindow : Window
    {
        private enum EditorMode
        {
            ListEmbedded,
            Create,
            Edit
        }

        private readonly StudentService _studentService = new();
        private readonly StudentRequirementService _requirementService = new();
        private readonly RequirementChecklistService _checklistService = new();
        private readonly EditorMode _mode;
        private readonly long? _editRequirementId;

        private List<Student> _students = new();
        private long? _selectedRequirementId;

        public StudentRequirementsWindow(long? preferredStudentId = null)
            : this(EditorMode.ListEmbedded, null, preferredStudentId)
        {
        }

        public StudentRequirementsWindow(bool createOnly, long? preferredStudentId = null)
            : this(createOnly ? EditorMode.Create : EditorMode.ListEmbedded, null, preferredStudentId)
        {
        }

        public StudentRequirementsWindow(long requirementId, long? preferredStudentId = null)
            : this(EditorMode.Edit, requirementId, preferredStudentId)
        {
        }

        private StudentRequirementsWindow(EditorMode mode, long? editRequirementId, long? preferredStudentId)
        {
            _mode = mode;
            _editRequirementId = editRequirementId;

            InitializeComponent();

            cboRequirement.ItemsSource = _checklistService.GetRequiredRequirements();
            cboRequirement.SelectedIndex = cboRequirement.Items.Count > 0 ? 0 : -1;

            cboRequirementStatus.ItemsSource = Enum.GetValues(typeof(RequirementChecklistStatus));
            cboRequirementStatus.SelectedItem = RequirementChecklistStatus.MISSING;
            dpSubmitted.SelectedDate = DateTime.Today;

            cboStudent.SelectionChanged += (_, _) =>
            {
                if (_mode == EditorMode.ListEmbedded)
                {
                    LoadRequirements();
                }
            };
            requirementsChecklistPanel.SelectionChanged += RequirementsChecklistPanel_SelectionChanged;
            requirementsChecklistPanel.MouseDoubleClick += (_, _) => OpenEditWindow();
            btnNew.Click += (_, _) => OpenCreateWindow();
            btnListEdit.Click += (_, _) => OpenEditWindow();
            btnListDelete.Click += (_, _) => DeleteRequirement();
            btnAdd.Click += (_, _) =>
            {
                if (_mode == EditorMode.Create)
                {
                    AddRequirement();
                }
                else if (_mode == EditorMode.Edit)
                {
                    SaveRequirement();
                }
                else
                {
                    OpenCreateWindow();
                }
            };
            btnSave.Click += (_, _) => SaveRequirement();
            btnDelete.Click += (_, _) => DeleteRequirement();
            btnCancel.Click += (_, _) =>
            {
                if (_mode == EditorMode.ListEmbedded)
                {
                    ClearEditor();
                }
                else
                {
                    Close();
                }
            };
            btnRefresh.Click += (_, _) =>
            {
                var selectedStudentId = cboStudent.SelectedValue is long id ? id : (long?)null;
                LoadStudents(selectedStudentId);
                if (_mode == EditorMode.ListEmbedded)
                {
                    LoadRequirements();
                }
            };

            LoadStudents(preferredStudentId);

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
                LoadRequirements();
            }
        }

        private void ConfigureListMode()
        {
            Title = "Student Requirements";
            sectionHeader.Title = "Student Requirements";
            sectionHeader.Subtitle = "Track required document submission and compliance notes";
            editorPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(requirementsChecklistPanel, 0);
            Grid.SetColumnSpan(requirementsChecklistPanel, 2);
            toolbarPanel.Visibility = Visibility.Visible;
            ClearEditor();
        }

        private void ConfigureModalEditorChrome()
        {
            toolbarPanel.Visibility = Visibility.Collapsed;
            requirementsChecklistPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(0);
            Width = 760;
            Height = 520;
            MinWidth = 760;
            MinHeight = 520;
        }

        private void ConfigureCreateMode()
        {
            Title = "Create Student Requirement";
            sectionHeader.Title = "Create Student Requirement";
            sectionHeader.Subtitle = "Add a requirement record for the selected student in a dedicated modal.";
            ConfigureModalEditorChrome();
            btnAdd.Content = "Create";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnCancel.Visibility = Visibility.Visible;
            ClearEditor();
        }

        private void ConfigureEditMode()
        {
            Title = "Edit Student Requirement";
            sectionHeader.Title = "Edit Student Requirement";
            sectionHeader.Subtitle = "Update requirement status, submitted date, and notes in a dedicated modal.";
            ConfigureModalEditorChrome();
            btnAdd.Content = "Save";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnCancel.Visibility = Visibility.Visible;
            LoadRequirementForEdit();
        }

        private void OpenCreateWindow()
        {
            var selectedStudentId = cboStudent.SelectedValue is long id ? id : (long?)null;
            var window = new StudentRequirementsWindow(true, selectedStudentId);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, toolbarPanel, requirementsChecklistPanel) == true)
            {
                LoadStudents(selectedStudentId);
                LoadRequirements();
            }
        }

        private void OpenEditWindow()
        {
            if (_mode != EditorMode.ListEmbedded)
            {
                return;
            }

            if (!_selectedRequirementId.HasValue)
            {
                MessageBox.Show("Select a requirement first.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedStudentId = cboStudent.SelectedValue is long id ? id : (long?)null;
            var window = new StudentRequirementsWindow(_selectedRequirementId.Value, selectedStudentId);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, toolbarPanel, requirementsChecklistPanel) == true)
            {
                LoadStudents(selectedStudentId);
                LoadRequirements();
                SelectRequirementById(_selectedRequirementId.Value);
            }
        }

        private void LoadStudents(long? preferredStudentId = null)
        {
            _students = _studentService.GetAll().OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToList();
            var items = _students.Select(s => new LookupItem(s.Id, $"{s.LastName}, {s.FirstName} ({s.Lrn})")).ToList();

            cboStudent.ItemsSource = items;
            if (preferredStudentId.HasValue && items.Any(x => x.Id == preferredStudentId.Value))
            {
                cboStudent.SelectedValue = preferredStudentId.Value;
            }
            else
            {
                cboStudent.SelectedIndex = items.Count > 0 ? 0 : -1;
            }
        }

        private void LoadRequirements()
        {
            if (cboStudent.SelectedValue is not long studentId)
            {
                requirementsChecklistPanel.Items = Array.Empty<RequirementChecklistItem>();
                requirementsChecklistPanel.SummaryText = "No student selected.";
                txtSummary.Text = "No student selected.";
                _selectedRequirementId = null;
                return;
            }

            var requirements = _requirementService.GetAll().Where(r => r.StudentId == studentId).ToList();
            var snapshot = _checklistService.BuildForStudent(studentId, requirements);

            requirementsChecklistPanel.Items = snapshot.Items;
            requirementsChecklistPanel.SummaryText = snapshot.SummaryText;
            txtSummary.Text = snapshot.SummaryText;
            _selectedRequirementId = null;
            requirementsChecklistPanel.SelectedItem = null;
        }

        private void RequirementsChecklistPanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = requirementsChecklistPanel.SelectedItem;
            _selectedRequirementId = selected?.RequirementId;

            if (_mode != EditorMode.ListEmbedded && selected != null)
            {
                cboRequirement.Text = selected.RequirementName;
                cboRequirementStatus.SelectedItem = selected.Status;
                dpSubmitted.SelectedDate = selected.SubmittedAt?.ToLocalTime().Date ?? DateTime.Today;
                txtNotes.Text = selected.Notes ?? string.Empty;
            }
        }

        private void AddRequirement()
        {
            if (cboStudent.SelectedValue is not long studentId)
            {
                MessageBox.Show("Select a student first.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var name = cboRequirement.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Requirement name is required.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var duplicate = _requirementService.GetAll()
                .Any(r => r.StudentId == studentId && string.Equals(r.RequirementName, name, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                MessageBox.Show("This requirement already exists for the selected student.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var status = cboRequirementStatus.SelectedItem is RequirementChecklistStatus selectedStatus
                ? selectedStatus
                : RequirementChecklistStatus.MISSING;

            var entity = BuildEntityFromEditor(new StudentRequirement
            {
                StudentId = studentId,
                RequirementName = name,
                CreatedAt = DateTime.UtcNow
            }, status);

            _requirementService.Create(entity);
            AuditTrailService.Log("CREATE", "student_requirements", entity.Id, null, entity);

            if (_mode == EditorMode.Create)
            {
                DialogResult = true;
                Close();
                return;
            }

            LoadRequirements();
            SelectRequirementById(entity.Id);
        }

        private void SaveRequirement()
        {
            if (!_selectedRequirementId.HasValue)
            {
                MessageBox.Show("Select a requirement first.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var entity = _requirementService.GetById(_selectedRequirementId.Value);
            if (entity == null)
            {
                return;
            }

            var status = cboRequirementStatus.SelectedItem is RequirementChecklistStatus selectedStatus
                ? selectedStatus
                : RequirementChecklistStatus.MISSING;

            var requirementName = cboRequirement.Text.Trim();
            if (string.IsNullOrWhiteSpace(requirementName))
            {
                MessageBox.Show("Requirement name is required.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var duplicate = _requirementService.GetAll()
                .Any(r => r.Id != entity.Id &&
                          r.StudentId == entity.StudentId &&
                          string.Equals(r.RequirementName, requirementName, StringComparison.OrdinalIgnoreCase));
            if (duplicate)
            {
                MessageBox.Show("This requirement already exists for the selected student.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var oldData = new { entity.RequirementName, entity.IsSubmitted, entity.SubmittedAt, entity.Notes };
            entity.RequirementName = requirementName;
            BuildEntityFromEditor(entity, status);

            _requirementService.Update(entity);
            AuditTrailService.Log("UPDATE", "student_requirements", entity.Id, oldData, entity);

            if (_mode == EditorMode.Edit)
            {
                DialogResult = true;
                Close();
                return;
            }

            LoadRequirements();
            SelectRequirementById(entity.Id);
        }

        private void DeleteRequirement()
        {
            if (!_selectedRequirementId.HasValue)
            {
                MessageBox.Show("Select a requirement first.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!AppFeedbackService.Confirm("Delete selected requirement?", "Confirm", this))
            {
                return;
            }

            var entity = _requirementService.GetById(_selectedRequirementId.Value);
            if (entity == null)
            {
                return;
            }

            _requirementService.Delete(_selectedRequirementId.Value);
            AuditTrailService.Log("DELETE", "student_requirements", _selectedRequirementId, entity, null);

            if (_mode != EditorMode.ListEmbedded)
            {
                DialogResult = true;
                Close();
                return;
            }

            LoadRequirements();
            ClearEditor();
        }

        private void LoadRequirementForEdit()
        {
            if (!_editRequirementId.HasValue)
            {
                MessageBox.Show("Requirement not supplied for edit mode.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            var entity = _requirementService.GetById(_editRequirementId.Value);
            if (entity == null)
            {
                MessageBox.Show("Requirement record not found.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            LoadStudents(entity.StudentId);
            cboStudent.SelectedValue = entity.StudentId;
            _selectedRequirementId = entity.Id;
            cboRequirement.Text = entity.RequirementName;

            var status = RequirementChecklistStatus.MISSING;
            if (entity.IsSubmitted)
            {
                status = entity.VerifiedByUserId.HasValue
                    ? RequirementChecklistStatus.VERIFIED
                    : RequirementChecklistStatus.SUBMITTED;
            }

            cboRequirementStatus.SelectedItem = status;
            dpSubmitted.SelectedDate = entity.SubmittedAt?.Date ?? DateTime.Today;
            txtNotes.Text = entity.Notes ?? string.Empty;
        }

        private StudentRequirement BuildEntityFromEditor(StudentRequirement entity, RequirementChecklistStatus status)
        {
            var normalizedNotes = _checklistService.BuildPersistedNotes(txtNotes.Text, status);
            var submittedDate = (dpSubmitted.SelectedDate ?? DateTime.Today).Date;
            var now = DateTime.UtcNow;

            entity.RequirementName = cboRequirement.Text.Trim();
            entity.IsSubmitted = status is RequirementChecklistStatus.SUBMITTED or RequirementChecklistStatus.VERIFIED;
            entity.SubmittedAt = entity.IsSubmitted ? submittedDate : null;
            entity.VerifiedByUserId = status == RequirementChecklistStatus.VERIFIED ? SessionContext.CurrentUser?.Id : null;
            entity.Notes = string.IsNullOrWhiteSpace(normalizedNotes) ? null : normalizedNotes;
            entity.UpdatedAt = now;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }

            return entity;
        }

        private void SelectRequirementById(long requirementId)
        {
            var item = (requirementsChecklistPanel.Items ?? Array.Empty<RequirementChecklistItem>())
                .FirstOrDefault(x => x.RequirementId == requirementId);
            if (item != null)
            {
                requirementsChecklistPanel.SelectedItem = item;
                _selectedRequirementId = requirementId;
            }
        }

        private void ClearEditor()
        {
            _selectedRequirementId = null;
            cboRequirement.SelectedIndex = cboRequirement.Items.Count > 0 ? 0 : -1;
            cboRequirement.Text = cboRequirement.SelectedItem?.ToString() ?? string.Empty;
            cboRequirementStatus.SelectedItem = RequirementChecklistStatus.MISSING;
            dpSubmitted.SelectedDate = DateTime.Today;
            txtNotes.Text = string.Empty;
            requirementsChecklistPanel.SelectedItem = null;
        }

        private sealed class LookupItem
        {
            public LookupItem(long id, string label)
            {
                Id = id;
                Label = label;
            }

            public long Id { get; }
            public string Label { get; }
        }
    }
}

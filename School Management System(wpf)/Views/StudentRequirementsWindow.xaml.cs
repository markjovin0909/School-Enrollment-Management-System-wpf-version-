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
        private readonly StudentService _studentService = new();
        private readonly StudentRequirementService _requirementService = new();
        private readonly RequirementChecklistService _checklistService = new();
        private readonly bool _createOnly;

        private List<Student> _students = new();
        private long? _selectedRequirementId;

        public StudentRequirementsWindow(long? preferredStudentId = null, bool createOnly = false)
        {
            _createOnly = createOnly;
            InitializeComponent();

            cboRequirement.ItemsSource = _checklistService.GetRequiredRequirements();
            cboRequirement.SelectedIndex = cboRequirement.Items.Count > 0 ? 0 : -1;

            cboRequirementStatus.ItemsSource = Enum.GetValues(typeof(RequirementChecklistStatus));
            cboRequirementStatus.SelectedItem = RequirementChecklistStatus.MISSING;
            dpSubmitted.SelectedDate = DateTime.Today;

            cboStudent.SelectionChanged += (_, _) => LoadRequirements();
            requirementsChecklistPanel.SelectionChanged += RequirementsChecklistPanel_SelectionChanged;
            btnNew.Click += (_, _) => OpenCreateWindow();
            btnAdd.Click += (_, _) =>
            {
                if (_createOnly)
                {
                    AddRequirement();
                }
                else
                {
                    OpenCreateWindow();
                }
            };
            btnSave.Click += (_, _) => SaveRequirement();
            btnDelete.Click += (_, _) => DeleteRequirement();
            btnCancel.Click += (_, _) => Close();
            btnRefresh.Click += (_, _) =>
            {
                var selectedStudentId = cboStudent.SelectedValue is long id ? id : (long?)null;
                LoadStudents(selectedStudentId);
                LoadRequirements();
            };

            LoadStudents(preferredStudentId);
            if (_createOnly)
            {
                ConfigureCreateMode();
            }
            else
            {
                LoadRequirements();
            }
        }

        private void OpenCreateWindow()
        {
            var selectedStudentId = cboStudent.SelectedValue is long id ? id : (long?)null;
            var window = new StudentRequirementsWindow(selectedStudentId, true) { Owner = this };
            if (window.ShowDialog() == true)
            {
                LoadStudents(selectedStudentId);
                LoadRequirements();
            }
        }

        private void ConfigureCreateMode()
        {
            Title = "Create Student Requirement";
            requirementsChecklistPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(0);
            btnNew.Visibility = Visibility.Collapsed;
            btnRefresh.Visibility = Visibility.Collapsed;
            txtSummary.Visibility = Visibility.Collapsed;
            btnAdd.Content = "Create";
            btnSave.Visibility = Visibility.Collapsed;
            btnDelete.Visibility = Visibility.Collapsed;
            btnCancel.Visibility = Visibility.Visible;
            Width = 760;
            Height = 520;
            MinWidth = 760;
            MinHeight = 520;
            LoadRequirements();
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
            cboRequirementStatus.SelectedItem = RequirementChecklistStatus.MISSING;
            dpSubmitted.SelectedDate = DateTime.Today;
            txtNotes.Text = string.Empty;
        }

        private void RequirementsChecklistPanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = requirementsChecklistPanel.SelectedItem;
            if (selected == null)
            {
                _selectedRequirementId = null;
                return;
            }

            _selectedRequirementId = selected.RequirementId;
            cboRequirement.Text = selected.RequirementName;
            cboRequirementStatus.SelectedItem = selected.Status;
            dpSubmitted.SelectedDate = selected.SubmittedAt?.ToLocalTime().Date ?? DateTime.Today;
            txtNotes.Text = selected.Notes ?? string.Empty;
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
            if (_createOnly)
            {
                DialogResult = true;
                Close();
                return;
            }

            LoadRequirements();
            SelectChecklistItem(entity.RequirementName);
        }

        private void SaveRequirement()
        {
            if (!_selectedRequirementId.HasValue)
            {
                // No existing record — create it instead (for MISSING requirements selected from checklist)
                var requirementName = cboRequirement.Text.Trim();
                if (!string.IsNullOrWhiteSpace(requirementName) && cboStudent.SelectedValue is long studentId)
                {
                    AddRequirement();
                    return;
                }

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

            var oldData = new { entity.RequirementName, entity.IsSubmitted, entity.SubmittedAt, entity.Notes };
            entity.RequirementName = cboRequirement.Text.Trim();
            BuildEntityFromEditor(entity, status);

            _requirementService.Update(entity);
            AuditTrailService.Log("UPDATE", "student_requirements", entity.Id, oldData, entity);
            LoadRequirements();
            SelectChecklistItem(entity.RequirementName);
        }

        private void DeleteRequirement()
        {
            if (!_selectedRequirementId.HasValue)
            {
                MessageBox.Show("Select a requirement first.", "Requirements", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show("Delete selected requirement?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            var entity = _requirementService.GetById(_selectedRequirementId.Value);
            _requirementService.Delete(_selectedRequirementId.Value);
            AuditTrailService.Log("DELETE", "student_requirements", _selectedRequirementId, entity, null);
            LoadRequirements();
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

        private void SelectChecklistItem(string requirementName)
        {
            var item = (requirementsChecklistPanel.Items ?? Array.Empty<RequirementChecklistItem>())
                .FirstOrDefault(x => string.Equals(x.RequirementName, requirementName, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                requirementsChecklistPanel.SelectedItem = item;
            }
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

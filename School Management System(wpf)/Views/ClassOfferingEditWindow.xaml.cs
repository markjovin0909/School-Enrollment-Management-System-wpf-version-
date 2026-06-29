using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class ClassOfferingEditWindow : Window
    {
        private const int TeacherLoadLimit = 8;

        private readonly ClassOfferingService _offeringService = new();
        private readonly SchoolYearService _schoolYearService = new();
        private readonly SectionService _sectionService = new();
        private readonly TeacherService _teacherService = new();
        private readonly SubjectService _subjectService = new();
        private readonly CurriculumService _curriculumService = new();
        private readonly long _offeringId;

        private List<TeacherOption> _teacherOptions = new();
        private ClassOffering? _offering;

        public long? SavedOfferingId { get; private set; }

        public ClassOfferingEditWindow(long offeringId)
        {
            _offeringId = offeringId;
            InitializeComponent();

            cboStatus.ItemsSource = Enum.GetValues(typeof(ClassOfferingStatus));
            btnSave.Click += (_, _) => SaveOffering();
            btnCancel.Click += (_, _) => Close();

            LoadLookups();
            LoadOffering();
        }

        private void LoadLookups()
        {
            var teachers = _teacherService.GetAll().ToList();
            _teacherOptions = new List<TeacherOption>
            {
                new() { Id = 0, Name = "(None)" }
            };
            _teacherOptions.AddRange(teachers.Select(t => new TeacherOption
            {
                Id = t.Id,
                Name = $"{t.LastName}, {t.FirstName}"
            }));

            cboTeacher.ItemsSource = _teacherOptions;
            cboTeacher.SelectedValue = 0L;
        }

        private void LoadOffering()
        {
            _offering = _offeringService.GetById(_offeringId);
            if (_offering == null)
            {
                AppFeedbackService.ShowWarning("Class offering record not found.", "Edit Offering", this);
                Close();
                return;
            }

            var subject = _subjectService.GetById(_offering.SubjectId);
            var section = _sectionService.GetById(_offering.SectionId);
            var schoolYear = _schoolYearService.GetById(_offering.SchoolYearId);
            var curriculum = _offering.CurriculumId.HasValue ? _curriculumService.GetById(_offering.CurriculumId.Value) : null;

            txtSubject.Text = subject?.Title ?? string.Empty;
            txtSection.Text = section?.Name ?? string.Empty;
            txtSchoolYear.Text = schoolYear?.Name ?? string.Empty;
            txtCurriculum.Text = curriculum?.Name ?? string.Empty;
            cboTeacher.SelectedValue = _offering.TeacherId ?? 0L;
            cboStatus.SelectedItem = _offering.Status;
            txtRoom.Text = _offering.Room ?? string.Empty;
        }

        private void SaveOffering()
        {
            if (_offering == null)
            {
                return;
            }

            HideValidationSummary();

            var teacherId = cboTeacher.SelectedValue is long selectedTeacherId ? selectedTeacherId : 0L;
            var errors = new List<string>();

            if (teacherId != 0)
            {
                var duplicateAssignment = _offeringService.GetAll().Any(o =>
                    o.Id != _offering.Id &&
                    o.SchoolYearId == _offering.SchoolYearId &&
                    o.SectionId == _offering.SectionId &&
                    o.SubjectId == _offering.SubjectId &&
                    o.TeacherId == teacherId);
                if (duplicateAssignment)
                {
                    errors.Add("Duplicate teacher assignment detected for this section and subject.");
                }

                var teacherLoad = _offeringService.GetAll().Count(o =>
                    o.Id != _offering.Id &&
                    o.SchoolYearId == _offering.SchoolYearId &&
                    o.TeacherId == teacherId &&
                    o.Status != ClassOfferingStatus.ARCHIVED);
                if (teacherLoad >= TeacherLoadLimit)
                {
                    errors.Add($"Teacher load limit reached ({TeacherLoadLimit}).");
                }
            }

            if (errors.Count > 0)
            {
                ShowValidationSummary(errors);
                return;
            }

            var oldData = new { _offering.TeacherId, _offering.Status, _offering.Room };
            _offering.TeacherId = teacherId == 0 ? null : teacherId;
            if (cboStatus.SelectedItem is ClassOfferingStatus status)
            {
                _offering.Status = status;
            }

            _offering.Room = string.IsNullOrWhiteSpace(txtRoom.Text) ? null : txtRoom.Text.Trim();
            _offering.UpdatedAt = DateTime.UtcNow;

            try
            {
                _offeringService.Update(_offering);
                AuditTrailService.Log("UPDATE", "class_offerings", _offering.Id, oldData, _offering);
                SavedOfferingId = _offering.Id;
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

        private sealed class TeacherOption
        {
            public long Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}

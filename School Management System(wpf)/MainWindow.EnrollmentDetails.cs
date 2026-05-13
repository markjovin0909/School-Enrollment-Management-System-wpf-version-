using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using School_Management_System.Models;
using School_Management_System.Services;
using School_Management_System.Views;

namespace School_Management_System
{
    public partial class MainWindow
    {
        private const int EnrollmentDetailsTabIndex = 12;

        private long? _enrollmentDetailId;
        private Enrollment? _enrollmentDetailRecord;

        private void InitializeEnrollmentDetailsTab()
        {
            // No additional wiring needed; data loads on navigation
        }

        private void OpenEnrollmentModal()
        {
            try
            {
                var modal = new EnrollmentModal { Owner = this };
                if (modal.ShowDialog() == true && modal.CreatedEnrollmentId.HasValue)
                {
                    NavigateToEnrollmentDetails(modal.CreatedEnrollmentId.Value);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open enrollment: {ex.Message}", "Enrollment", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavigateToEnrollmentDetails(long enrollmentId)
        {
            _enrollmentDetailId = enrollmentId;
            LoadEnrollmentDetailData();
            NavigateMainTab(EnrollmentDetailsTabIndex);
        }

        private void LoadEnrollmentDetailData()
        {
            if (!_enrollmentDetailId.HasValue) return;

            _enrollmentDetailRecord = _enrollmentService.GetById(_enrollmentDetailId.Value);
            if (_enrollmentDetailRecord == null)
            {
                MessageBox.Show("Enrollment not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                NavigateMainTab(0);
                return;
            }

            var student = _studentService.GetById(_enrollmentDetailRecord.StudentId);
            var schoolYear = _schoolYearService.GetAll().FirstOrDefault(sy => sy.Id == _enrollmentDetailRecord.SchoolYearId);
            var gradeLevel = _gradeLevelService.GetAll().FirstOrDefault(g => g.Id == _enrollmentDetailRecord.GradeLevelId);
            var section = _sectionService.GetAll().FirstOrDefault(s => s.Id == _enrollmentDetailRecord.SectionId);
            var curriculum = _curriculumService.GetAll().FirstOrDefault(c => c.Id == _enrollmentDetailRecord.CurriculumId);

            var studentName = student != null
                ? $"{student.LastName}, {student.FirstName}{(string.IsNullOrWhiteSpace(student.MiddleName) ? "" : $" {student.MiddleName}")}"
                : "Unknown";

            sectionEDHeader.Title = $"Enrollment - {studentName}";
            sectionEDHeader.Subtitle = $"Enrolled {_enrollmentDetailRecord.EnrolledAt:yyyy-MM-dd}  |  Status: {_enrollmentDetailRecord.Status}";

            txtEDSchoolYear.Text = schoolYear?.Name ?? "-";
            txtEDGradeLevel.Text = gradeLevel != null ? (gradeLevel.Code ?? gradeLevel.Name) : "-";
            txtEDSection.Text = section?.Name ?? "-";
            txtEDCurriculum.Text = curriculum?.Name ?? "-";
            txtEDStatus.Text = _enrollmentDetailRecord.Status.ToString();
            txtEDEnrolledAt.Text = _enrollmentDetailRecord.EnrolledAt.ToString("yyyy-MM-dd HH:mm");
            txtEDStudentName.Text = studentName;
            txtEDStudentLrn.Text = student?.Lrn ?? "-";

            LoadEnrollmentDetailRequirements(student?.Id);
            LoadEnrollmentDetailSubjects(_enrollmentDetailRecord.SectionId);
        }

        private void LoadEnrollmentDetailRequirements(long? studentId)
        {
            if (!studentId.HasValue)
            {
                gridEDRequirements.ItemsSource = null;
                return;
            }

            try
            {
                var requirementService = new StudentRequirementService();
                var requirements = requirementService.GetAll()
                    .Where(r => r.StudentId == studentId.Value)
                    .Select(r => new EDRequirementRow
                    {
                        Name = r.RequirementName,
                        Status = r.IsSubmitted ? "Submitted" : "Not Submitted",
                        SubmittedDate = r.SubmittedAt?.ToString("yyyy-MM-dd") ?? ""
                    })
                    .ToList();

                gridEDRequirements.ItemsSource = requirements;
            }
            catch (Exception ex)
            {
                gridEDRequirements.ItemsSource = null;
                System.Diagnostics.Debug.WriteLine($"Load enrollment requirements failed: {ex.Message}");
            }
        }

        private void LoadEnrollmentDetailSubjects(long sectionId)
        {
            try
            {
                var subjects = _subjectService.GetAll().ToDictionary(s => s.Id);
                var teachers = _teacherService.GetAll().ToDictionary(t => t.Id);

                var offerings = _classOfferingService.GetAll()
                    .Where(o => o.SectionId == sectionId && o.Status != ClassOfferingStatus.ARCHIVED)
                    .Select(o =>
                    {
                        var subjectName = subjects.TryGetValue(o.SubjectId, out var subj) ? subj.Title : "Unknown";
                        var teacherName = o.TeacherId.HasValue && teachers.TryGetValue(o.TeacherId.Value, out var t)
                            ? $"{t.LastName}, {t.FirstName}" : "Unassigned";
                        return new EDSubjectRow
                        {
                            SubjectName = subjectName,
                            TeacherName = teacherName,
                            Status = o.Status.ToString()
                        };
                    })
                    .ToList();

                gridEDSubjects.ItemsSource = offerings;
            }
            catch (Exception ex)
            {
                gridEDSubjects.ItemsSource = null;
                System.Diagnostics.Debug.WriteLine($"Load enrollment subjects failed: {ex.Message}");
            }
        }

        public class EDRequirementRow
        {
            public string Name { get; set; } = "";
            public string Status { get; set; } = "";
            public string SubmittedDate { get; set; } = "";
        }

        public class EDSubjectRow
        {
            public string SubjectName { get; set; } = "";
            public string TeacherName { get; set; } = "";
            public string Status { get; set; } = "";
        }
    }
}

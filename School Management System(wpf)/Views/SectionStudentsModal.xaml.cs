using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class SectionStudentsModal : Window
    {
        private readonly ClassStudentService _classStudentService = new();
        private readonly ClassOfferingService _classOfferingService = new();
        private readonly StudentService _studentService = new();

        public SectionStudentsModal(long sectionId, string sectionName)
        {
            InitializeComponent();

            txtTitle.Text = $"Students in {sectionName}";
            txtSubtitle.Text = "Students enrolled in class offerings for this section";
            btnClose.Click += (_, _) => Close();

            LoadStudents(sectionId);
        }

        private void LoadStudents(long sectionId)
        {
            try
            {
                var offerings = _classOfferingService.GetAll()
                    .Where(o => o.SectionId == sectionId)
                    .Select(o => o.Id)
                    .ToHashSet();

                var studentIds = _classStudentService.GetAll()
                    .Where(cs => offerings.Contains(cs.ClassOfferingId) && cs.Status == ClassStudentStatus.ACTIVE)
                    .Select(cs => cs.StudentId)
                    .Distinct()
                    .ToList();

                var students = _studentService.GetAll()
                    .Where(s => studentIds.Contains(s.Id))
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .Select(s => new StudentRow
                    {
                        StudentNumber = s.StudentNumber ?? "",
                        Lrn = s.Lrn ?? "",
                        FullName = $"{s.LastName}, {s.FirstName}{(string.IsNullOrWhiteSpace(s.MiddleName) ? "" : $" {s.MiddleName}")}",
                        Status = s.Status.ToString()
                    })
                    .ToList();

                gridStudents.ItemsSource = students;
                txtSummary.Text = $"{students.Count} student(s) found in this section.";
            }
            catch (Exception ex)
            {
                txtSummary.Text = $"Failed to load students: {ex.Message}";
            }
        }

        public class StudentRow
        {
            public string StudentNumber { get; set; } = "";
            public string Lrn { get; set; } = "";
            public string FullName { get; set; } = "";
            public string Status { get; set; } = "";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class StudentSearchModal : Window
    {
        private readonly StudentService _studentService = new();
        private List<StudentListItem> _allStudents = new();

        /// <summary>
        /// The student ID selected by the user (null if none selected).
        /// </summary>
        public long? SelectedStudentId { get; private set; }

        /// <summary>
        /// If true, the user chose to add a new student (open the create form).
        /// </summary>
        public bool OpenAddStudent { get; private set; }

        public StudentSearchModal()
        {
            InitializeComponent();

            btnClose.Click += (_, _) => Close();
            btnAddStudent.Click += BtnAddStudent_Click;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            lstStudents.MouseDoubleClick += LstStudents_MouseDoubleClick;
            KeyDown += OnKeyDown;

            LoadStudents();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void LoadStudents()
        {
            try
            {
                _allStudents = _studentService.GetAll()
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .Select(s => new StudentListItem
                    {
                        Id = s.Id,
                        FullName = $"{s.LastName}, {s.FirstName}{(string.IsNullOrWhiteSpace(s.MiddleName) ? "" : $" {s.MiddleName}")}",
                        Lrn = s.Lrn ?? "",
                        StudentNumber = s.StudentNumber ?? ""
                    })
                    .ToList();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load students: {ex.Message}", "Students", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var term = (txtSearch.Text ?? "").Trim();
            List<StudentListItem> filtered;

            if (string.IsNullOrWhiteSpace(term))
            {
                filtered = _allStudents;
            }
            else
            {
                filtered = _allStudents.Where(s =>
                    s.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    s.Lrn.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    s.StudentNumber.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            lstStudents.ItemsSource = filtered;

            if (filtered.Count == 0 && !string.IsNullOrWhiteSpace(term))
            {
                txtNoResults.Visibility = Visibility.Visible;
                lstStudents.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtNoResults.Visibility = Visibility.Collapsed;
                lstStudents.Visibility = Visibility.Visible;
            }

            txtFooterInfo.Text = $"{filtered.Count} of {_allStudents.Count} students";
        }

        private void LstStudents_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstStudents.SelectedItem is StudentListItem selected)
            {
                SelectedStudentId = selected.Id;
                DialogResult = true;
                Close();
            }
        }

        private void BtnAddStudent_Click(object sender, RoutedEventArgs e)
        {
            // Open create window directly from the modal
            var createWindow = new StudentCreateWindow { Owner = this };
            if (createWindow.ShowDialog() == true && createWindow.CreatedStudentId.HasValue)
            {
                // Student was created — select it and close
                SelectedStudentId = createWindow.CreatedStudentId.Value;
                DialogResult = true;
                Close();
            }
            else
            {
                // User cancelled — refresh the list in case anything changed
                LoadStudents();
            }
        }

        public class StudentListItem
        {
            public long Id { get; set; }
            public string FullName { get; set; } = "";
            public string Lrn { get; set; } = "";
            public string StudentNumber { get; set; } = "";
        }
    }
}

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
    public partial class TeacherSearchModal : Window
    {
        private readonly TeacherService _teacherService = new();
        private List<TeacherListItem> _allTeachers = new();

        public long? SelectedTeacherId { get; private set; }
        public bool OpenAddTeacher { get; private set; }

        public TeacherSearchModal()
        {
            InitializeComponent();

            btnClose.Click += (_, _) => Close();
            btnAddTeacher.Click += BtnAddTeacher_Click;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            lstTeachers.MouseDoubleClick += LstTeachers_MouseDoubleClick;
            KeyDown += OnKeyDown;

            LoadTeachers();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }

        private void LoadTeachers()
        {
            try
            {
                _allTeachers = _teacherService.GetAll()
                    .OrderBy(t => t.LastName)
                    .ThenBy(t => t.FirstName)
                    .Select(t => new TeacherListItem
                    {
                        Id = t.Id,
                        FullName = $"{t.LastName}, {t.FirstName}{(string.IsNullOrWhiteSpace(t.MiddleName) ? "" : $" {t.MiddleName}")}",
                        EmployeeNo = t.EmployeeNo ?? "",
                        Specialization = t.Specialization ?? ""
                    })
                    .ToList();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load teachers: {ex.Message}", "Teachers", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            var term = (txtSearch.Text ?? "").Trim();
            List<TeacherListItem> filtered;

            if (string.IsNullOrWhiteSpace(term))
                filtered = _allTeachers;
            else
                filtered = _allTeachers.Where(t =>
                    t.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    t.EmployeeNo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    t.Specialization.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            lstTeachers.ItemsSource = filtered;

            if (filtered.Count == 0 && !string.IsNullOrWhiteSpace(term))
            {
                txtNoResults.Visibility = Visibility.Visible;
                lstTeachers.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtNoResults.Visibility = Visibility.Collapsed;
                lstTeachers.Visibility = Visibility.Visible;
            }

            txtFooterInfo.Text = $"{filtered.Count} of {_allTeachers.Count} teachers";
        }

        private void LstTeachers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstTeachers.SelectedItem is TeacherListItem selected)
            {
                SelectedTeacherId = selected.Id;
                DialogResult = true;
                Close();
            }
        }

        private void BtnAddTeacher_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new TeacherCreateWindow { Owner = this };
            if (createWindow.ShowDialog() == true && createWindow.CreatedTeacherId.HasValue)
            {
                SelectedTeacherId = createWindow.CreatedTeacherId.Value;
                DialogResult = true;
                Close();
            }
            else
            {
                LoadTeachers();
            }
        }

        public class TeacherListItem
        {
            public long Id { get; set; }
            public string FullName { get; set; } = "";
            public string EmployeeNo { get; set; } = "";
            public string Specialization { get; set; } = "";
        }
    }
}

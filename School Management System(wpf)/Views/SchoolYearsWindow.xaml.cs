using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class SchoolYearsWindow : Window
    {
        private readonly SchoolYearService _schoolYearService = new();
        private readonly SchoolSettingService _schoolSettingService = new();
        private readonly EditorMode _mode;
        private DataTable _table = new();
        private long? _selectedId;
        private long? _editId;

        private enum EditorMode
        {
            ListEmbedded,
            Create,
            Edit
        }

        public SchoolYearsWindow()
            : this(EditorMode.ListEmbedded, null)
        {
        }

        public SchoolYearsWindow(bool createOnly)
            : this(createOnly ? EditorMode.Create : EditorMode.ListEmbedded, null)
        {
        }

        public SchoolYearsWindow(long editId)
            : this(EditorMode.Edit, editId)
        {
        }

        private SchoolYearsWindow(EditorMode mode, long? editId)
        {
            _mode = mode;
            _editId = editId;
            InitializeComponent();

            cboStatus.ItemsSource = Enum.GetValues(typeof(SchoolYearStatus));
            cboStatus.SelectedItem = SchoolYearStatus.PLANNING;

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            gridSchoolYears.AutoGeneratingColumn += (_, e) =>
            {
                if (e.PropertyName == "Id" || e.PropertyName == "Enrollment Close" || e.PropertyName == "Archived")
                {
                    e.Cancel = true;
                }
            };
            gridSchoolYears.SelectionChanged += GridSchoolYears_SelectionChanged;
            gridSchoolYears.MouseDoubleClick += (_, _) => OpenEditWindow();
            btnNew.Click += (_, _) => OpenCreateWindow();
            btnListEdit.Click += (_, _) => OpenEditWindow();
            btnListDelete.Click += (_, _) => ArchiveOrRestoreSchoolYear();
            btnRefresh.Click += (_, _) => LoadData();
            btnAdd.Click += (_, _) =>
            {
                if (_mode == EditorMode.Create)
                {
                    AddSchoolYear();
                }
                else if (_mode == EditorMode.Edit)
                {
                    SaveSchoolYear();
                }
                else
                {
                    OpenCreateWindow();
                }
            };
            btnSave.Click += (_, _) => SaveSchoolYear();
            btnArchiveRestore.Click += (_, _) => ArchiveOrRestoreSchoolYear();
            btnClear.Click += (_, _) =>
            {
                if (_mode != EditorMode.ListEmbedded)
                {
                    Close();
                }
                else
                {
                    ClearEditor();
                }
            };

            dpStart.SelectedDate = DateTime.Today;
            dpEnd.SelectedDate = DateTime.Today;
            dpEnrollOpen.SelectedDate = DateTime.Today;
            dpEnrollClose.SelectedDate = DateTime.Today;

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
                LoadData();
            }
        }

        private void ConfigureListMode()
        {
            editorPanel.Visibility = Visibility.Collapsed;
            Grid.SetColumn(searchPanel, 0);
            Grid.SetColumnSpan(searchPanel, 2);
            searchPanel.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetColumnSpan(listPanel, 2);
            listPanel.Margin = new Thickness(0);
        }

        private void OpenCreateWindow()
        {
            var window = new SchoolYearsWindow(true);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, listPanel, searchPanel) == true)
            {
                LoadData();
            }
        }

        private void OpenEditWindow()
        {
            if (_mode != EditorMode.ListEmbedded || !_selectedId.HasValue)
            {
                if (_mode == EditorMode.ListEmbedded)
                {
                    MessageBox.Show("Select a school year first.", "School Year", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            var editId = _selectedId.Value;
            var window = new SchoolYearsWindow(editId);
            if (AppFeedbackService.ShowOwnedDialog(window, this, editorPanel, listPanel, searchPanel) == true)
            {
                LoadData(editId);
            }
        }

        private void ConfigureModalEditorChrome()
        {
            searchPanel.Visibility = Visibility.Collapsed;
            gridSchoolYears.Visibility = Visibility.Collapsed;
            Grid.SetColumn(editorPanel, 0);
            Grid.SetColumnSpan(editorPanel, 2);
            editorPanel.Margin = new Thickness(0);
            Width = 620;
            Height = 520;
            MinWidth = 620;
            MinHeight = 520;
        }

        private void ConfigureCreateMode()
        {
            Title = "Create School Year";
            ConfigureModalEditorChrome();
            btnAdd.Content = "Create";
            btnSave.Visibility = Visibility.Collapsed;
            btnArchiveRestore.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            ClearEditor();
        }

        private void ConfigureEditMode()
        {
            Title = "Edit School Year";
            ConfigureModalEditorChrome();
            btnAdd.Content = "Save";
            btnSave.Visibility = Visibility.Collapsed;
            btnArchiveRestore.Visibility = Visibility.Collapsed;
            btnClear.Content = "Cancel";
            LoadEditorFromEntity();
        }

        private void LoadEditorFromEntity()
        {
            if (!_editId.HasValue)
            {
                return;
            }

            var schoolYear = _schoolYearService.GetById(_editId.Value);
            if (schoolYear == null)
            {
                MessageBox.Show("School year not found.", "School Year", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            _selectedId = schoolYear.Id;
            txtName.Text = schoolYear.Name;
            chkStart.IsChecked = schoolYear.StartDate.HasValue;
            dpStart.SelectedDate = schoolYear.StartDate?.Date ?? DateTime.Today;
            chkEnd.IsChecked = schoolYear.EndDate.HasValue;
            dpEnd.SelectedDate = schoolYear.EndDate?.Date ?? DateTime.Today;
            chkEnrollOpen.IsChecked = schoolYear.EnrollmentOpenDate.HasValue;
            dpEnrollOpen.SelectedDate = schoolYear.EnrollmentOpenDate?.Date ?? DateTime.Today;
            chkEnrollClose.IsChecked = schoolYear.EnrollmentCloseDate.HasValue;
            dpEnrollClose.SelectedDate = schoolYear.EnrollmentCloseDate?.Date ?? DateTime.Today;
            cboStatus.SelectedItem = schoolYear.Status;
        }

        private void LoadData(long? preferredId = null)
        {
            _table = new DataTable();
            _table.Columns.Add("Id", typeof(long));
            _table.Columns.Add("Name");
            _table.Columns.Add("Start");
            _table.Columns.Add("End");
            _table.Columns.Add("Enrollment Open");
            _table.Columns.Add("Enrollment Close");
            _table.Columns.Add("Status");
            _table.Columns.Add("Archived");

            foreach (var sy in _schoolYearService.GetAll())
            {
                _table.Rows.Add(
                    sy.Id,
                    sy.Name,
                    sy.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    sy.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    sy.EnrollmentOpenDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    sy.EnrollmentCloseDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    sy.Status.ToString(),
                    sy.IsArchived ? "YES" : "NO");
            }

            gridSchoolYears.ItemsSource = _table.DefaultView;
            ApplyFilter(preferredId);
        }

        private void ApplyFilter(long? preferredId = null)
        {
            var term = (txtSearch.Text ?? string.Empty).Trim().Replace("'", "''");
            _table.DefaultView.RowFilter = string.IsNullOrWhiteSpace(term)
                ? string.Empty
                : $"Name LIKE '%{term}%' OR Start LIKE '%{term}%' OR [End] LIKE '%{term}%' OR [Enrollment Open] LIKE '%{term}%' OR [Enrollment Close] LIKE '%{term}%' OR Status LIKE '%{term}%' OR Archived LIKE '%{term}%'";

            if (preferredId.HasValue && SelectRow(preferredId.Value))
            {
                return;
            }

            _selectedId = null;
            gridSchoolYears.SelectedItem = null;
        }

        private bool SelectRow(long id)
        {
            foreach (var item in gridSchoolYears.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("Id") == id)
                {
                    gridSchoolYears.SelectedItem = item;
                    gridSchoolYears.ScrollIntoView(item);
                    _selectedId = id;
                    return true;
                }
            }

            return false;
        }

        private void GridSchoolYears_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridSchoolYears.SelectedItem is not DataRowView row)
            {
                _selectedId = null;
                return;
            }

            _selectedId = row.Row.Field<long>("Id");
            var schoolYear = _schoolYearService.GetById(_selectedId.Value);
            if (schoolYear == null)
            {
                return;
            }

            txtName.Text = schoolYear.Name;

            chkStart.IsChecked = schoolYear.StartDate.HasValue;
            dpStart.SelectedDate = schoolYear.StartDate?.Date ?? DateTime.Today;
            chkEnd.IsChecked = schoolYear.EndDate.HasValue;
            dpEnd.SelectedDate = schoolYear.EndDate?.Date ?? DateTime.Today;
            chkEnrollOpen.IsChecked = schoolYear.EnrollmentOpenDate.HasValue;
            dpEnrollOpen.SelectedDate = schoolYear.EnrollmentOpenDate?.Date ?? DateTime.Today;
            chkEnrollClose.IsChecked = schoolYear.EnrollmentCloseDate.HasValue;
            dpEnrollClose.SelectedDate = schoolYear.EnrollmentCloseDate?.Date ?? DateTime.Today;
            cboStatus.SelectedItem = schoolYear.Status;
        }

        private void AddSchoolYear()
        {
            var entity = BuildEntityFromEditor();
            if (entity == null)
            {
                return;
            }

            if (!ConfirmActiveSwitch(entity.Status, null))
            {
                return;
            }

            try
            {
                _schoolYearService.Create(entity);
                AuditTrailService.Log("CREATE", "school_years", entity.Id, null, entity);
                if (_mode == EditorMode.Create)
                {
                    DialogResult = true;
                    Close();
                    return;
                }

                LoadData(entity.Id);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "School Year", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create school year: {ex.Message}", "School Year", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSchoolYear()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a school year first.", "Update", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sy = _schoolYearService.GetById(_selectedId.Value);
            if (sy == null)
            {
                return;
            }

            var status = cboStatus.SelectedItem is SchoolYearStatus selectedStatus ? selectedStatus : sy.Status;
            if (!ConfirmActiveSwitch(status, sy.Id))
            {
                return;
            }

            var oldData = new
            {
                sy.Name,
                sy.StartDate,
                sy.EndDate,
                sy.EnrollmentOpenDate,
                sy.EnrollmentCloseDate,
                sy.Status,
                sy.IsArchived
            };

            sy.Name = txtName.Text.Trim();
            sy.StartDate = chkStart.IsChecked == true ? (dpStart.SelectedDate ?? DateTime.Today).Date : null;
            sy.EndDate = chkEnd.IsChecked == true ? (dpEnd.SelectedDate ?? DateTime.Today).Date : null;
            sy.EnrollmentOpenDate = chkEnrollOpen.IsChecked == true ? (dpEnrollOpen.SelectedDate ?? DateTime.Today).Date : null;
            sy.EnrollmentCloseDate = chkEnrollClose.IsChecked == true ? (dpEnrollClose.SelectedDate ?? DateTime.Today).Date : null;
            sy.Status = status;

            try
            {
                _schoolYearService.Update(sy);
                AuditTrailService.Log("UPDATE", "school_years", sy.Id, oldData, sy);
                if (_mode == EditorMode.Edit)
                {
                    DialogResult = true;
                    Close();
                    return;
                }

                LoadData(sy.Id);
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "School Year", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update school year: {ex.Message}", "School Year", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ArchiveOrRestoreSchoolYear()
        {
            if (!_selectedId.HasValue)
            {
                MessageBox.Show("Select a school year first.", "School Year", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var sy = _schoolYearService.GetById(_selectedId.Value);
            if (sy == null)
            {
                return;
            }

            var confirmMessage = sy.IsArchived
                ? "Restore selected school year?"
                : "Archive selected school year? Historical records will remain available.";
            if (!AppFeedbackService.Confirm(confirmMessage, "Confirm", this))
            {
                return;
            }

            try
            {
                if (sy.IsArchived)
                {
                    _schoolYearService.Restore(sy.Id);
                    AuditTrailService.Log("RESTORE", "school_years", sy.Id, new { sy.IsArchived }, new { IsArchived = false });
                }
                else
                {
                    AuditTrailService.Log("ARCHIVE", "school_years", sy.Id, sy, new { IsArchived = true });
                    _schoolYearService.Delete(sy.Id);
                }

                LoadData();
                ClearEditor();
            }
            catch (DomainValidationException ex)
            {
                MessageBox.Show(ex.Message, "School Year", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed school year operation: {ex.Message}", "School Year", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearEditor()
        {
            _selectedId = null;
            txtName.Clear();
            chkStart.IsChecked = false;
            chkEnd.IsChecked = false;

            // Prefill enrollment window from system settings so new school years inherit defaults.
            var settings = _schoolSettingService.GetLatest();
            if (settings?.EnrollmentOpenDate.HasValue == true)
            {
                chkEnrollOpen.IsChecked = true;
                dpEnrollOpen.SelectedDate = settings.EnrollmentOpenDate.Value.Date;
            }
            else
            {
                chkEnrollOpen.IsChecked = false;
                dpEnrollOpen.SelectedDate = DateTime.Today;
            }

            if (settings?.EnrollmentCloseDate.HasValue == true)
            {
                chkEnrollClose.IsChecked = true;
                dpEnrollClose.SelectedDate = settings.EnrollmentCloseDate.Value.Date;
            }
            else
            {
                chkEnrollClose.IsChecked = false;
                dpEnrollClose.SelectedDate = DateTime.Today;
            }

            cboStatus.SelectedItem = SchoolYearStatus.PLANNING;
            gridSchoolYears.SelectedItem = null;
        }

        private SchoolYear? BuildEntityFromEditor()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("School year name is required.", "School Year", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }

            var status = cboStatus.SelectedItem is SchoolYearStatus selectedStatus
                ? selectedStatus
                : SchoolYearStatus.PLANNING;

            return new SchoolYear
            {
                Name = txtName.Text.Trim(),
                StartDate = chkStart.IsChecked == true ? (dpStart.SelectedDate ?? DateTime.Today).Date : null,
                EndDate = chkEnd.IsChecked == true ? (dpEnd.SelectedDate ?? DateTime.Today).Date : null,
                EnrollmentOpenDate = chkEnrollOpen.IsChecked == true ? (dpEnrollOpen.SelectedDate ?? DateTime.Today).Date : null,
                EnrollmentCloseDate = chkEnrollClose.IsChecked == true ? (dpEnrollClose.SelectedDate ?? DateTime.Today).Date : null,
                Status = status
            };
        }

        private bool ConfirmActiveSwitch(SchoolYearStatus nextStatus, long? currentSchoolYearId)
        {
            if (nextStatus != SchoolYearStatus.ACTIVE)
            {
                return true;
            }

            var currentActive = _schoolYearService.GetActiveSchoolYear();
            if (currentActive == null || (currentSchoolYearId.HasValue && currentActive.Id == currentSchoolYearId.Value))
            {
                return true;
            }

            var confirm = MessageBox.Show(
                $"'{currentActive.Name}' is currently ACTIVE. Continuing will set it to CLOSED and activate this school year. Continue?",
                "Set Active School Year",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            return confirm == MessageBoxResult.Yes;
        }
    }
}

using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class StudentAccountsWindow : Window
    {
        private readonly User _viewer;
        private readonly Window? _dialogOwner;
        private readonly StudentAccountService _studentAccountService = new();
        private readonly StudentService _studentService = new();

        private DataTable _table = new();
        private long? _selectedStudentId;

        public StudentAccountsWindow(User viewer, long? preferredStudentId = null, Window? dialogOwner = null)
        {
            _viewer = viewer;
            _dialogOwner = dialogOwner;
            InitializeComponent();

            cboHealth.ItemsSource = new[] { "ALL", "SYNCED", "NEEDS_SYNC" };
            cboHealth.SelectedIndex = 0;

            cboStatus.Items.Add("ALL");
            foreach (var status in Enum.GetValues(typeof(UserStatus)))
            {
                cboStatus.Items.Add(status.ToString());
            }
            cboStatus.SelectedIndex = 0;

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            cboHealth.SelectionChanged += (_, _) => ApplyFilter();
            cboStatus.SelectionChanged += (_, _) => ApplyFilter();
            gridAccounts.AutoGeneratingColumn += (_, e) =>
            {
                if (e.PropertyName == "StudentId" ||
                    e.PropertyName == "UserId" ||
                    e.PropertyName == "LRN" ||
                    e.PropertyName == "Login Enabled")
                {
                    e.Cancel = true;
                }
            };
            gridAccounts.SelectionChanged += GridAccounts_SelectionChanged;

            btnSetActive.Click += (_, _) => SetStatus(UserStatus.ACTIVE);
            btnSetInactive.Click += (_, _) => SetStatus(UserStatus.INACTIVE);
            btnSync.Click += (_, _) => SyncAccount();
            btnReset.Click += (_, _) => ResetAccount();
            btnHistory.Click += (_, _) => ViewHistory();
            btnRefresh.Click += (_, _) => LoadData(_selectedStudentId);

            LoadData(preferredStudentId);
        }

        private void LoadData(long? preferredStudentId = null)
        {
            _table = new DataTable();
            _table.Columns.Add("StudentId", typeof(long));
            _table.Columns.Add("UserId", typeof(long));
            _table.Columns.Add("Student No");
            _table.Columns.Add("Account ID");
            _table.Columns.Add("Student");
            _table.Columns.Add("LRN");
            _table.Columns.Add("Student Status");
            _table.Columns.Add("Account Status");
            _table.Columns.Add("Health");
            _table.Columns.Add("Login Enabled");
            _table.Columns.Add("Updated");

            foreach (var account in _studentAccountService.GetAll())
            {
                _table.Rows.Add(
                    account.StudentId,
                    account.UserId,
                    account.StudentNumber,
                    account.AccountId,
                    account.StudentName,
                    account.Lrn,
                    account.StudentStatus.ToString(),
                    account.Status.ToString(),
                    ResolveHealth(account),
                    account.CanLogin ? "Yes" : "No",
                    account.UpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            }

            gridAccounts.ItemsSource = _table.DefaultView;
            ApplyFilter(preferredStudentId);
        }

        private void ApplyFilter(long? preferredStudentId = null)
        {
            var term = (txtSearch.Text ?? string.Empty).Trim().Replace("'", "''");
            var clauses = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(term))
            {
                clauses.Add($"[Student No] LIKE '%{term}%' OR [Account ID] LIKE '%{term}%' OR Student LIKE '%{term}%' OR LRN LIKE '%{term}%'");
            }

            var health = cboHealth.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(health) && !string.Equals(health, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                clauses.Add($"Health = '{health.Replace("'", "''")}'");
            }

            var status = cboStatus.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "ALL", StringComparison.OrdinalIgnoreCase))
            {
                clauses.Add($"[Account Status] = '{status.Replace("'", "''")}'");
            }

            _table.DefaultView.RowFilter = clauses.Count == 0 ? string.Empty : string.Join(" AND ", clauses);

            if (preferredStudentId.HasValue && SelectRowByStudentId(preferredStudentId.Value))
            {
                return;
            }

            _selectedStudentId = null;
            gridAccounts.SelectedItem = null;
        }

        private bool SelectRowByStudentId(long studentId)
        {
            foreach (var item in gridAccounts.Items)
            {
                if (item is DataRowView row && row.Row.Field<long>("StudentId") == studentId)
                {
                    gridAccounts.SelectedItem = item;
                    gridAccounts.ScrollIntoView(item);
                    _selectedStudentId = studentId;
                    return true;
                }
            }

            return false;
        }

        private void GridAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (gridAccounts.SelectedItem is not DataRowView row)
            {
                _selectedStudentId = null;
                ClearDetails();
                return;
            }

            _selectedStudentId = row.Row.Field<long>("StudentId");
            PopulateDetails(row);
        }

        private void SetStatus(UserStatus status)
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student account first.", "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = _studentAccountService.SetStudentAccountStatus(_selectedStudentId.Value, status);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadData(_selectedStudentId);
        }

        private void SyncAccount()
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student account first.", "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = _studentAccountService.SyncStudentAccount(_selectedStudentId.Value);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(result.Message, "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadData(_selectedStudentId);
        }

        private void ResetAccount()
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student account first.", "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = _studentAccountService.ResetStudentAccount(_selectedStudentId.Value);
            if (!result.Success)
            {
                MessageBox.Show(result.Message, "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(result.Message, "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadData(_selectedStudentId);
        }

        private void ViewHistory()
        {
            if (!_selectedStudentId.HasValue)
            {
                MessageBox.Show("Select a student account first.", "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var student = _studentService.GetById(_selectedStudentId.Value);
            if (student == null)
            {
                MessageBox.Show("Student record not found.", "Student Accounts", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var label = $"Student Account {student.LastName}, {student.FirstName} ({student.StudentNumber})";
            var historyWindow = new UserActivityHistoryWindow(_viewer, "student_accounts", student.Id, label);
            AppFeedbackService.ShowOwnedDialog(historyWindow, ResolveHistoryOwner(), gridAccounts);
        }

        private Window? ResolveHistoryOwner()
        {
            if (_dialogOwner != null && _dialogOwner.IsVisible)
            {
                return _dialogOwner;
            }

            if (IsVisible)
            {
                return this;
            }

            return Application.Current?.MainWindow;
        }

        private static string ResolveHealth(StudentAccountService.StudentAccountSummary account)
        {
            return account.Role == UserRole.STUDENT &&
                   !account.CanLogin &&
                   string.Equals(account.AccountId, account.StudentNumber, StringComparison.OrdinalIgnoreCase) &&
                   account.Status == account.StudentStatus
                ? "SYNCED"
                : "NEEDS_SYNC";
        }

        private void PopulateDetails(DataRowView row)
        {
            txtStudentNameValue.Text = row.Row["Student"]?.ToString() ?? "-";
            txtStudentNoValue.Text = row.Row["Student No"]?.ToString() ?? "-";
            txtAccountIdValue.Text = row.Row["Account ID"]?.ToString() ?? "-";
            txtLrnValue.Text = row.Row["LRN"]?.ToString() ?? "-";
            txtStudentStatusValue.Text = row.Row["Student Status"]?.ToString() ?? "-";
            txtAccountStatusValue.Text = row.Row["Account Status"]?.ToString() ?? "-";
            txtHealthValue.Text = row.Row["Health"]?.ToString() ?? "-";
            txtLoginEnabledValue.Text = row.Row["Login Enabled"]?.ToString() ?? "-";
            txtUpdatedValue.Text = row.Row["Updated"]?.ToString() ?? "-";
        }

        private void ClearDetails()
        {
            txtStudentNameValue.Text = "-";
            txtStudentNoValue.Text = "-";
            txtAccountIdValue.Text = "-";
            txtLrnValue.Text = "-";
            txtStudentStatusValue.Text = "-";
            txtAccountStatusValue.Text = "-";
            txtHealthValue.Text = "-";
            txtLoginEnabledValue.Text = "-";
            txtUpdatedValue.Text = "-";
        }
    }
}

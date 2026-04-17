using System;
using System.Data;
using System.Linq;
using System.Windows;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Views
{
    public partial class UserActivityHistoryWindow : Window
    {
        private enum HistoryScope
        {
            User,
            Student,
            StudentAccount,
            Teacher,
            Enrollment
        }

        private readonly User _viewer;
        private readonly long _targetUserId;
        private readonly long _targetEntityId;
        private readonly string _targetLabel;
        private readonly HistoryScope _scope;
        private readonly AuditLogService _auditLogService = new();
        private readonly UserService _userService = new();
        private DataTable _table = new();

        public UserActivityHistoryWindow(User viewer, long targetUserId, string targetLabel)
        {
            InitializeComponent();
            _viewer = viewer;
            _targetUserId = targetUserId;
            _targetEntityId = 0;
            _targetLabel = targetLabel;
            _scope = HistoryScope.User;
            Title = $"Activity History - {_targetLabel}";
            lblViewing.Text = $"Viewing: {_targetLabel}";

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            btnRefresh.Click += (_, _) => LoadData();

            LoadData();
        }

        public UserActivityHistoryWindow(User viewer)
            : this(viewer, viewer.Id, viewer.Username)
        {
        }

        public UserActivityHistoryWindow(User viewer, string entityType, long entityId, string targetLabel)
        {
            InitializeComponent();
            _viewer = viewer;
            _targetUserId = 0;
            _targetEntityId = entityId;
            _targetLabel = targetLabel;
            _scope = ParseScope(entityType);
            Title = $"Activity History - {_targetLabel}";
            lblViewing.Text = $"Viewing: {_targetLabel}";

            txtSearch.TextChanged += (_, _) => ApplyFilter();
            btnRefresh.Click += (_, _) => LoadData();

            LoadData();
        }

        private void LoadData()
        {
            _table = new DataTable();
            _table.Columns.Add("Date");
            _table.Columns.Add("Action");
            _table.Columns.Add("Entity");
            _table.Columns.Add("Entity ID");
            _table.Columns.Add("By User");
            _table.Columns.Add("Details");

            var logs = _auditLogService.GetAll()
                .Where(IncludeLog)
                .OrderByDescending(x => x.CreatedAt)
                .Take(500)
                .ToList();

            foreach (var log in logs)
            {
                var actor = _userService.GetById(log.UserId)?.Username ?? $"User {log.UserId}";
                _table.Rows.Add(
                    log.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    log.Action,
                    log.Entity,
                    log.EntityId?.ToString() ?? string.Empty,
                    actor,
                    Compact(log.Payload));
            }

            gridHistory.ItemsSource = _table.DefaultView;
            ApplyFilter();
        }

        private bool IncludeLog(AuditLog log)
        {
            return _scope switch
            {
                HistoryScope.User => log.UserId == _targetUserId,
                HistoryScope.Student => IsStudentLog(log),
                HistoryScope.StudentAccount => IsStudentAccountLog(log),
                HistoryScope.Teacher => IsTeacherLog(log),
                HistoryScope.Enrollment => IsEnrollmentLog(log),
                _ => false
            };
        }

        private bool IsStudentLog(AuditLog log)
        {
            if (EntityIdMatch(log, "students", _targetEntityId))
            {
                return true;
            }

            return EntityPayloadContainsId(log, "StudentId", _targetEntityId)
                || EntityPayloadContainsId(log, "student_id", _targetEntityId)
                || EntityPayloadContainsId(log, "studentId", _targetEntityId);
        }

        private bool IsTeacherLog(AuditLog log)
        {
            if (EntityIdMatch(log, "teachers", _targetEntityId))
            {
                return true;
            }

            return EntityPayloadContainsId(log, "TeacherId", _targetEntityId)
                || EntityPayloadContainsId(log, "teacher_id", _targetEntityId)
                || EntityPayloadContainsId(log, "teacherId", _targetEntityId);
        }

        private bool IsStudentAccountLog(AuditLog log)
        {
            if (EntityIdMatch(log, "student_accounts", _targetEntityId))
            {
                return true;
            }

            return EntityPayloadContainsId(log, "StudentId", _targetEntityId)
                || EntityPayloadContainsId(log, "student_id", _targetEntityId)
                || EntityPayloadContainsId(log, "studentId", _targetEntityId);
        }

        private bool IsEnrollmentLog(AuditLog log)
        {
            if (EntityIdMatch(log, "enrollments", _targetEntityId))
            {
                return true;
            }

            return EntityPayloadContainsId(log, "EnrollmentId", _targetEntityId)
                || EntityPayloadContainsId(log, "enrollment_id", _targetEntityId)
                || EntityPayloadContainsId(log, "enrollmentId", _targetEntityId);
        }

        private static bool EntityIdMatch(AuditLog log, string entity, long entityId)
        {
            return string.Equals(log.Entity, entity, StringComparison.OrdinalIgnoreCase) &&
                   log.EntityId.HasValue &&
                   log.EntityId.Value == entityId;
        }

        private static bool EntityPayloadContainsId(AuditLog log, string key, long id)
        {
            if (string.IsNullOrWhiteSpace(log.Payload))
            {
                return false;
            }

            var payload = log.Payload ?? string.Empty;
            return payload.Contains($"\"{key}\":{id}", StringComparison.OrdinalIgnoreCase) ||
                   payload.Contains($"\"{key}\": {id}", StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyFilter()
        {
            var term = (txtSearch.Text ?? string.Empty).Trim().Replace("'", "''");
            _table.DefaultView.RowFilter = string.IsNullOrWhiteSpace(term)
                ? string.Empty
                : $"Action LIKE '%{term}%' OR Entity LIKE '%{term}%' OR Details LIKE '%{term}%'";
        }

        private static string Compact(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var compact = text.Replace(Environment.NewLine, " ").Trim();
            if (compact.Length > 240)
            {
                compact = compact.Substring(0, 240) + "...";
            }

            return compact;
        }

        private static HistoryScope ParseScope(string? entityType)
        {
            return (entityType ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "student" or "students" => HistoryScope.Student,
                "student_account" or "student_accounts" => HistoryScope.StudentAccount,
                "teacher" or "teachers" => HistoryScope.Teacher,
                "enrollment" or "enrollments" => HistoryScope.Enrollment,
                _ => HistoryScope.User
            };
        }
    }
}

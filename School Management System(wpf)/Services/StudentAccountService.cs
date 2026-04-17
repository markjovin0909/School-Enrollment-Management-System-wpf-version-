using System;
using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal sealed class StudentAccountService
    {
        internal sealed class StudentAccountSummary
        {
            public long StudentId { get; init; }
            public long UserId { get; init; }
            public string AccountId { get; init; } = string.Empty;
            public string StudentNumber { get; init; } = string.Empty;
            public string StudentName { get; init; } = string.Empty;
            public string Lrn { get; init; } = string.Empty;
            public UserRole Role { get; init; }
            public UserStatus StudentStatus { get; init; }
            public UserStatus Status { get; init; }
            public bool CanLogin { get; init; }
            public DateTime UpdatedAt { get; init; }
        }

        public IEnumerable<StudentAccountSummary> GetAll()
        {
            using var db = new AppDbContext();
            return BuildAccountSummaries(db);
        }

        public StudentAccountSummary? GetByStudentId(long studentId)
        {
            using var db = new AppDbContext();
            return BuildAccountSummaries(db).FirstOrDefault(x => x.StudentId == studentId);
        }

        public StudentAccountSummary? GetByUserId(long userId)
        {
            using var db = new AppDbContext();
            return BuildAccountSummaries(db).FirstOrDefault(x => x.UserId == userId);
        }

        public OperationResult<User> CreateManagedAccount(string preferredAccountId, UserStatus status)
        {
            using var db = new AppDbContext();

            var accountId = ResolveUniqueAccountId(db, preferredAccountId, null);
            var now = DateTime.UtcNow;
            var user = new User
            {
                Username = accountId,
                PasswordHash = PasswordHasher.HashPassword(BuildSystemPassword()),
                Role = UserRole.STUDENT,
                CanLogin = false,
                Status = status,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.Users.Add(user);
            db.SaveChanges();

            AuditTrailService.Log("CREATE", "users", user.Id, null, new
            {
                user.Username,
                user.Role,
                user.Status,
                user.CanLogin,
                Source = "StudentAccountService"
            });

            return OperationResult<User>.Ok(user, "Student account created.");
        }

        public OperationResult<User> SyncStudentAccount(long studentId)
        {
            using var db = new AppDbContext();
            var student = db.Students.Find(studentId);
            if (student == null)
            {
                return OperationResult<User>.Fail("Student record was not found.");
            }

            var user = db.Users.Find(student.UserId);
            if (user == null)
            {
                return OperationResult<User>.Fail("Linked student account was not found.");
            }

            var oldData = Snapshot(student, user);
            var desiredAccountId = ResolveUniqueAccountId(db, student.StudentNumber, user.Id);
            user.Username = desiredAccountId;
            user.Role = UserRole.STUDENT;
            user.CanLogin = false;
            user.Status = student.Status;
            user.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();

            AuditTrailService.Log("UPDATE", "student_accounts", student.Id, oldData, Snapshot(student, user));
            return OperationResult<User>.Ok(user, "Student account synchronized.");
        }

        public OperationResult<User> SetStudentAccountStatus(long studentId, UserStatus status)
        {
            using var db = new AppDbContext();
            var student = db.Students.Find(studentId);
            if (student == null)
            {
                return OperationResult<User>.Fail("Student record was not found.");
            }

            var user = db.Users.Find(student.UserId);
            if (user == null)
            {
                return OperationResult<User>.Fail("Linked student account was not found.");
            }

            var oldData = Snapshot(student, user);
            var oldStudentStatus = student.Status;
            student.Status = status;
            student.UpdatedAt = DateTime.UtcNow;
            user.Status = status;
            user.Role = UserRole.STUDENT;
            user.CanLogin = false;
            user.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();

            AuditTrailService.Log("UPDATE", "students", student.Id, new { Status = oldStudentStatus }, new { student.Status });
            AuditTrailService.Log("UPDATE", "student_accounts", student.Id, oldData, Snapshot(student, user));
            return OperationResult<User>.Ok(user, $"Student account status updated to {status}.");
        }

        public OperationResult<User> ResetStudentAccount(long studentId)
        {
            using var db = new AppDbContext();
            var student = db.Students.Find(studentId);
            if (student == null)
            {
                return OperationResult<User>.Fail("Student record was not found.");
            }

            var user = db.Users.Find(student.UserId);
            if (user == null)
            {
                return OperationResult<User>.Fail("Linked student account was not found.");
            }

            var oldData = Snapshot(student, user);
            user.Username = ResolveUniqueAccountId(db, student.StudentNumber, user.Id);
            user.PasswordHash = PasswordHasher.HashPassword(BuildSystemPassword());
            user.Role = UserRole.STUDENT;
            user.CanLogin = false;
            user.Status = student.Status;
            user.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();

            AuditTrailService.Log("RESET", "student_accounts", student.Id, oldData, Snapshot(student, user));
            return OperationResult<User>.Ok(user, "Student account details reset to system defaults.");
        }

        private static IEnumerable<StudentAccountSummary> BuildAccountSummaries(AppDbContext db)
        {
            return db.Students
                .Join(
                    db.Users,
                    student => student.UserId,
                    user => user.Id,
                    (student, user) => new
                    {
                        StudentId = student.Id,
                        UserId = user.Id,
                        AccountId = user.Username,
                        StudentNumber = student.StudentNumber,
                        LastName = student.LastName,
                        FirstName = student.FirstName,
                        Lrn = student.Lrn,
                        Role = user.Role,
                        StudentStatus = student.Status,
                        Status = user.Status,
                        CanLogin = user.CanLogin,
                        UpdatedAt = user.UpdatedAt
                    })
                .ToList()
                .Select(x => new StudentAccountSummary
                {
                    StudentId = x.StudentId,
                    UserId = x.UserId,
                    AccountId = x.AccountId,
                    StudentNumber = x.StudentNumber,
                    StudentName = $"{x.LastName}, {x.FirstName}",
                    Lrn = x.Lrn,
                    Role = x.Role,
                    StudentStatus = x.StudentStatus,
                    Status = x.Status,
                    CanLogin = x.CanLogin,
                    UpdatedAt = x.UpdatedAt
                })
                .OrderBy(x => x.StudentName)
                .ToList();
        }

        private static string BuildSystemPassword()
        {
            return $"Managed!{Guid.NewGuid():N}";
        }

        private static string ResolveUniqueAccountId(AppDbContext db, string preferredAccountId, long? excludeUserId)
        {
            var baseId = string.IsNullOrWhiteSpace(preferredAccountId)
                ? $"STU-{Guid.NewGuid():N}"[..12]
                : preferredAccountId.Trim().ToUpperInvariant();

            var accountId = baseId;
            var counter = 2;
            while (db.Users.Any(x =>
                       (!excludeUserId.HasValue || x.Id != excludeUserId.Value) &&
                       string.Equals(x.Username, accountId, StringComparison.OrdinalIgnoreCase)))
            {
                accountId = $"{baseId}-{counter++}";
            }

            return accountId;
        }

        private static object Snapshot(Student student, User user)
        {
            return new
            {
                StudentId = student.Id,
                UserId = user.Id,
                AccountId = user.Username,
                StudentNumber = student.StudentNumber,
                StudentStatus = student.Status,
                AccountStatus = user.Status,
                user.CanLogin,
                user.Role
            };
        }
    }
}

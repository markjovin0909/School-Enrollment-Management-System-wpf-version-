using System;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class AuthService
    {
        public OperationResult<User> Authenticate(string username, string password)
        {
            var normalizedUsername = (username ?? string.Empty).Trim();
            if (LoginAttemptService.IsLocked(normalizedUsername, out var remaining))
            {
                return OperationResult<User>.Fail($"Too many failed login attempts. Try again in {Math.Ceiling(remaining.TotalMinutes)} minute(s).");
            }

            using var db = new AppDbContext();
            var repo = new UserRepository(db);
            var user = repo.GetByUsername(normalizedUsername);
            if (user == null || !PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                LoginAttemptService.RecordFailure(normalizedUsername);
                AuditTrailService.LogWithActor(
                    user?.Id,
                    "LOGIN_FAILED",
                    "users",
                    user?.Id,
                    null,
                    new { Username = normalizedUsername });
                return OperationResult<User>.Fail("Invalid username or password.");
            }

            if (!user.CanLogin)
            {
                LoginAttemptService.RecordFailure(normalizedUsername);
                AuditTrailService.LogWithActor(
                    user.Id,
                    "LOGIN_BLOCKED",
                    "users",
                    user.Id,
                    null,
                    new { user.Username, Reason = "ACCOUNT_LOGIN_DISABLED" });
                return OperationResult<User>.Fail("This account is for managed records only and cannot be used to sign in.");
            }

            if (user.Role != UserRole.SUPERADMIN)
            {
                LoginAttemptService.RecordFailure(normalizedUsername);
                AuditTrailService.LogWithActor(
                    user.Id,
                    "LOGIN_BLOCKED",
                    "users",
                    user.Id,
                    null,
                    new { user.Username, user.Role, Reason = "NON_SUPERADMIN_ROLE" });
                return OperationResult<User>.Fail("Only SUPERADMIN accounts are allowed to sign in.");
            }

            if (user.Status == UserStatus.LOCKED)
            {
                return OperationResult<User>.Fail("Account is locked. Please contact an administrator.");
            }

            if (user.Status != UserStatus.ACTIVE)
            {
                return OperationResult<User>.Fail("Account is inactive. Please contact an administrator.");
            }

            user.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();
            LoginAttemptService.RecordSuccess(normalizedUsername);
            AuditTrailService.LogWithActor(user.Id, "LOGIN_SUCCESS", "users", user.Id, null, new { user.Username });
            return OperationResult<User>.Ok(user);
        }

        public OperationResult<User> Register(User user, string password)
        {
            using var db = new AppDbContext();
            var repo = new UserRepository(db);
            if (repo.ExistsUsername(user.Username))
            {
                return OperationResult<User>.Fail("Username already exists.");
            }

            var passwordValidation = PasswordPolicyService.Validate(password, user.Username);
            if (!passwordValidation.Success)
            {
                return OperationResult<User>.Fail(passwordValidation.Message);
            }

            user.PasswordHash = PasswordHasher.HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            if (user.Role == default)
            {
                user.Role = UserRole.SUPERADMIN;
            }

            db.Users.Add(user);
            db.SaveChanges();
            return OperationResult<User>.Ok(user, "Registration successful.");
        }
    }
}

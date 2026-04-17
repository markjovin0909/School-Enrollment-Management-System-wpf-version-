using System;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal class AccountSecurityService
    {
        public OperationResult<bool> ChangePassword(long userId, string currentPassword, string newPassword)
        {
            using var db = new AppDbContext();
            var user = db.Users.Find(userId);
            if (user == null) return OperationResult<bool>.Fail("User not found.");
            if (!PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash))
            {
                return OperationResult<bool>.Fail("Current password is incorrect.");
            }

            var passwordValidation = PasswordPolicyService.Validate(newPassword, user.Username);
            if (!passwordValidation.Success)
            {
                return OperationResult<bool>.Fail(passwordValidation.Message);
            }

            user.PasswordHash = PasswordHasher.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();
            AuditTrailService.LogWithActor(SessionContext.CurrentUser?.Id ?? user.Id, "PASSWORD_CHANGE", "users", user.Id, null, new { user.Username });
            return OperationResult<bool>.Ok(true, "Password changed.");
        }

        public OperationResult<bool> ResetPassword(long userId, string newPassword)
        {
            using var db = new AppDbContext();
            var user = db.Users.Find(userId);
            if (user == null) return OperationResult<bool>.Fail("User not found.");

            var passwordValidation = PasswordPolicyService.Validate(newPassword, user.Username);
            if (!passwordValidation.Success)
            {
                return OperationResult<bool>.Fail(passwordValidation.Message);
            }

            user.PasswordHash = PasswordHasher.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();
            AuditTrailService.LogWithActor(SessionContext.CurrentUser?.Id ?? user.Id, "PASSWORD_RESET", "users", user.Id, null, new { user.Username });
            return OperationResult<bool>.Ok(true, "Password reset.");
        }

        public OperationResult<bool> RecoverPassword(
            string username,
            UserRole role,
            string identityValue,
            string lastName,
            string newPassword)
        {
            using var db = new AppDbContext();
            var normalizedUsername = (username ?? string.Empty).Trim();
            var normalizedIdentity = (identityValue ?? string.Empty).Trim();
            var normalizedLastName = (lastName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedUsername) ||
                string.IsNullOrWhiteSpace(normalizedIdentity) ||
                string.IsNullOrWhiteSpace(normalizedLastName))
            {
                return OperationResult<bool>.Fail("Complete username, identity, and last name.");
            }

            if (role != UserRole.SUPERADMIN)
            {
                return OperationResult<bool>.Fail("Only SUPERADMIN account recovery is supported.");
            }

            var user = db.Users.FirstOrDefault(u =>
                u.Role == UserRole.SUPERADMIN &&
                u.Username.ToLower() == normalizedUsername.ToLower());
            if (user == null)
            {
                return OperationResult<bool>.Fail("SUPERADMIN account not found.");
            }

            var schoolCode = db.SchoolSettings
                .OrderByDescending(s => s.Id)
                .Select(s => s.SchoolCode)
                .FirstOrDefault() ?? string.Empty;

            var isMatch =
                !string.IsNullOrWhiteSpace(schoolCode) &&
                schoolCode.Equals(normalizedIdentity, StringComparison.OrdinalIgnoreCase) &&
                normalizedLastName.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase);

            if (!isMatch)
            {
                return OperationResult<bool>.Fail("Identity verification failed.");
            }

            var passwordValidation = PasswordPolicyService.Validate(newPassword, user.Username);
            if (!passwordValidation.Success)
            {
                return OperationResult<bool>.Fail(passwordValidation.Message);
            }

            user.PasswordHash = PasswordHasher.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();

            AuditTrailService.LogWithActor(user.Id, "PASSWORD_RECOVERY", "users", user.Id, null, new { user.Username, user.Role });
            return OperationResult<bool>.Ok(true, "Password recovered successfully.");
        }

        public void LogLogout(User user, string reason = "USER_LOGOUT")
        {
            AuditTrailService.LogWithActor(user.Id, "LOGOUT", "users", user.Id, null, new { user.Username, Reason = reason });
        }
    }
}

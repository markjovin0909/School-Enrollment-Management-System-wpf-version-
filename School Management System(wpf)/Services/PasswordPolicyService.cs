using System;
using System.Linq;

namespace School_Management_System.Services
{
    internal static class PasswordPolicyService
    {
        public static OperationResult<bool> Validate(string password, string? username = null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return OperationResult<bool>.Fail("Password is required.");
            }

            if (password.Length < 12)
            {
                return OperationResult<bool>.Fail("Password must be at least 12 characters.");
            }

            if (password.Any(char.IsWhiteSpace))
            {
                return OperationResult<bool>.Fail("Password must not contain spaces.");
            }

            if (!password.Any(char.IsUpper))
            {
                return OperationResult<bool>.Fail("Password must include at least one uppercase letter.");
            }

            if (!password.Any(char.IsLower))
            {
                return OperationResult<bool>.Fail("Password must include at least one lowercase letter.");
            }

            if (!password.Any(char.IsDigit))
            {
                return OperationResult<bool>.Fail("Password must include at least one number.");
            }

            if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            {
                return OperationResult<bool>.Fail("Password must include at least one special character.");
            }

            var normalizedUsername = (username ?? string.Empty).Trim();
            if (normalizedUsername.Length >= 3 &&
                password.Contains(normalizedUsername, StringComparison.OrdinalIgnoreCase))
            {
                return OperationResult<bool>.Fail("Password must not contain your username.");
            }

            return OperationResult<bool>.Ok(true);
        }
    }
}

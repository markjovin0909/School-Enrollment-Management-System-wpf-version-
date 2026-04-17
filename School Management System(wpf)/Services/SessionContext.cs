using System;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal static class SessionContext
    {
        private static User? _currentUser;

        public static User? CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                if (_currentUser != null)
                {
                    LoginAtUtc = DateTime.UtcNow;
                    LastActivityUtc = DateTime.UtcNow;
                }
            }
        }

        public static DateTime? LoginAtUtc { get; private set; }
        public static DateTime? LastActivityUtc { get; private set; }

        public static TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(20);

        public static void Touch()
        {
            if (_currentUser == null)
            {
                return;
            }

            LastActivityUtc = DateTime.UtcNow;
        }

        public static bool IsExpired()
        {
            if (_currentUser == null || !LastActivityUtc.HasValue)
            {
                return false;
            }

            return DateTime.UtcNow - LastActivityUtc.Value >= IdleTimeout;
        }

        public static void Clear()
        {
            _currentUser = null;
            LoginAtUtc = null;
            LastActivityUtc = null;
        }
    }
}

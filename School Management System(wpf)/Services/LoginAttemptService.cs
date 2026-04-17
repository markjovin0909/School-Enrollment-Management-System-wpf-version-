using System;
using System.Collections.Concurrent;

namespace School_Management_System.Services
{
    internal static class LoginAttemptService
    {
        private sealed class AttemptState
        {
            public int FailedAttempts { get; set; }
            public DateTime? LockedUntilUtc { get; set; }
        }

        private static readonly ConcurrentDictionary<string, AttemptState> Attempts = new(StringComparer.OrdinalIgnoreCase);

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

        public static bool IsLocked(string username, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            if (!Attempts.TryGetValue(username.Trim(), out var state) || !state.LockedUntilUtc.HasValue)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            if (state.LockedUntilUtc.Value <= now)
            {
                state.FailedAttempts = 0;
                state.LockedUntilUtc = null;
                return false;
            }

            remaining = state.LockedUntilUtc.Value - now;
            return true;
        }

        public static void RecordFailure(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            var key = username.Trim();
            var state = Attempts.GetOrAdd(key, _ => new AttemptState());
            state.FailedAttempts++;
            if (state.FailedAttempts >= MaxFailedAttempts)
            {
                state.LockedUntilUtc = DateTime.UtcNow.Add(LockDuration);
            }
        }

        public static void RecordSuccess(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return;
            }

            Attempts.TryRemove(username.Trim(), out _);
        }
    }
}

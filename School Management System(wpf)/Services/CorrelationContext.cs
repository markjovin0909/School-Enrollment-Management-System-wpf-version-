using System;
using System.Threading;

namespace School_Management_System.Services
{
    internal static class CorrelationContext
    {
        private static readonly AsyncLocal<string?> CurrentCorrelationId = new();

        public static string? CurrentId => CurrentCorrelationId.Value;

        public static string Ensure()
        {
            if (!string.IsNullOrWhiteSpace(CurrentCorrelationId.Value))
            {
                return CurrentCorrelationId.Value!;
            }

            CurrentCorrelationId.Value = BuildId();
            return CurrentCorrelationId.Value!;
        }

        public static IDisposable BeginScope(string? correlationId = null)
        {
            var previous = CurrentCorrelationId.Value;
            CurrentCorrelationId.Value = string.IsNullOrWhiteSpace(correlationId)
                ? BuildId()
                : correlationId.Trim();

            return new Scope(() => CurrentCorrelationId.Value = previous);
        }

        public static IDisposable? BeginScopeIfMissing()
        {
            if (!string.IsNullOrWhiteSpace(CurrentCorrelationId.Value))
            {
                return null;
            }

            return BeginScope();
        }

        private static string BuildId()
        {
            return $"REQ-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}".Substring(0, 36);
        }

        private sealed class Scope : IDisposable
        {
            private readonly Action _onDispose;
            private bool _disposed;

            public Scope(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _onDispose();
            }
        }
    }
}

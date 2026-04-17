using System;
using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal sealed class OperationalMetricsDashboardService
    {
        private readonly EnrollmentService _enrollmentService = new();
        private readonly EnrollmentQueueSlaService _slaService = new();
        private readonly BackupRestoreService _backupRestoreService = new();
        private readonly ExceptionQueueService _exceptionQueueService = new();

        public OperationalMetricsSnapshot BuildSnapshot(DateTime? referenceUtc = null)
        {
            var now = referenceUtc ?? DateTime.UtcNow;
            var startCurrent = now.AddDays(-7);
            var startPrevious = now.AddDays(-14);

            var enrollments = _enrollmentService.GetAll().ToList();
            var policy = _slaService.LoadPolicy();
            var queueAgingCurrent = enrollments
                .Select(x => _slaService.Evaluate(x, now, policy))
                .Count(x => x.Severity is EnrollmentQueueSlaSeverity.Warning or EnrollmentQueueSlaSeverity.Critical);

            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            var transitions = db.EnrollmentStateTransitions.ToList();

            var queueAgingWindowCurrent = transitions.Count(x =>
                x.CreatedAt >= startCurrent &&
                (x.NewStatus == EnrollmentStatus.PENDING || x.NewStatus == EnrollmentStatus.RESERVED));
            var queueAgingWindowPrevious = transitions.Count(x =>
                x.CreatedAt >= startPrevious &&
                x.CreatedAt < startCurrent &&
                (x.NewStatus == EnrollmentStatus.PENDING || x.NewStatus == EnrollmentStatus.RESERVED));

            var decisionReversalCurrent = transitions.Count(x =>
                x.CreatedAt >= startCurrent &&
                x.PreviousStatus == EnrollmentStatus.ENROLLED &&
                x.NewStatus != EnrollmentStatus.ENROLLED);
            var decisionReversalPrevious = transitions.Count(x =>
                x.CreatedAt >= startPrevious &&
                x.CreatedAt < startCurrent &&
                x.PreviousStatus == EnrollmentStatus.ENROLLED &&
                x.NewStatus != EnrollmentStatus.ENROLLED);

            var waitlisted = enrollments.Where(x => x.Status == EnrollmentStatus.RESERVED).ToList();
            var waitlistCurrent = waitlisted.Count;
            var waitlistPressureCurrent = transitions.Count(x =>
                x.CreatedAt >= startCurrent &&
                x.NewStatus == EnrollmentStatus.RESERVED);
            var waitlistPressurePrevious = transitions.Count(x =>
                x.CreatedAt >= startPrevious &&
                x.CreatedAt < startCurrent &&
                x.NewStatus == EnrollmentStatus.RESERVED);

            var failedHistory = _backupRestoreService.LoadHistory(240)
                .Where(x => !string.Equals(x.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
                .ToList();
            var failedCurrent = failedHistory.Count(x => x.TimestampUtc >= startCurrent);
            var failedPrevious = failedHistory.Count(x => x.TimestampUtc >= startPrevious && x.TimestampUtc < startCurrent);
            var openCriticalExceptions = _exceptionQueueService.GetActive()
                .Count(x => x.Severity == ExceptionQueueSeverity.CRITICAL);

            return new OperationalMetricsSnapshot
            {
                QueueAging = BuildMetric(
                    "Queue Aging",
                    queueAgingCurrent,
                    queueAgingWindowCurrent - queueAgingWindowPrevious,
                    queueAgingCurrent > 0 ? "warning" : "normal"),
                DecisionReversals = BuildMetric(
                    "Decision Reversals (7d)",
                    decisionReversalCurrent,
                    decisionReversalCurrent - decisionReversalPrevious,
                    decisionReversalCurrent > 0 ? "warning" : "normal"),
                WaitlistPressure = BuildMetric(
                    "Waitlist Pressure",
                    waitlistCurrent,
                    waitlistPressureCurrent - waitlistPressurePrevious,
                    waitlistCurrent > 0 ? "warning" : "normal"),
                FailedCriticalOps = BuildMetric(
                    "Failed Critical Ops",
                    failedCurrent + openCriticalExceptions,
                    failedCurrent - failedPrevious,
                    failedCurrent + openCriticalExceptions > 0 ? "critical" : "normal")
            };
        }

        private static OperationalTrendMetric BuildMetric(string title, int value, int trendDelta, string severity)
        {
            var trendPrefix = trendDelta > 0 ? "+" : string.Empty;
            var trend = trendDelta == 0 ? "No change vs previous 7 days" : $"{trendPrefix}{trendDelta} vs previous 7 days";
            return new OperationalTrendMetric
            {
                Title = title,
                Value = value.ToString(),
                Trend = trend,
                Severity = severity
            };
        }
    }

    internal sealed class OperationalMetricsSnapshot
    {
        public OperationalTrendMetric QueueAging { get; set; } = new();
        public OperationalTrendMetric DecisionReversals { get; set; } = new();
        public OperationalTrendMetric WaitlistPressure { get; set; } = new();
        public OperationalTrendMetric FailedCriticalOps { get; set; } = new();

        public IReadOnlyList<OperationalTrendMetric> All => new[]
        {
            QueueAging,
            DecisionReversals,
            WaitlistPressure,
            FailedCriticalOps
        };
    }

    internal sealed class OperationalTrendMetric
    {
        public string Title { get; set; } = string.Empty;
        public string Value { get; set; } = "0";
        public string Trend { get; set; } = string.Empty;
        public string Severity { get; set; } = "normal";
    }
}

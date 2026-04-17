using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal enum EnrollmentQueueSlaSeverity
    {
        None = 0,
        OnTrack = 1,
        Warning = 2,
        Critical = 3
    }

    internal sealed class EnrollmentQueueSlaPolicy
    {
        private readonly HashSet<EnrollmentStatus> _trackedStatuses;

        public EnrollmentQueueSlaPolicy(int warningHours, int criticalHours, IEnumerable<EnrollmentStatus> trackedStatuses)
        {
            WarningHours = Math.Max(1, warningHours);
            CriticalHours = Math.Max(WarningHours + 1, criticalHours);
            _trackedStatuses = new HashSet<EnrollmentStatus>(trackedStatuses ?? Array.Empty<EnrollmentStatus>());
            if (_trackedStatuses.Count == 0)
            {
                _trackedStatuses.Add(EnrollmentStatus.PENDING);
                _trackedStatuses.Add(EnrollmentStatus.RESERVED);
            }
        }

        public static EnrollmentQueueSlaPolicy Default { get; } = new(
            warningHours: 24,
            criticalHours: 72,
            trackedStatuses: new[] { EnrollmentStatus.PENDING, EnrollmentStatus.RESERVED });

        public int WarningHours { get; }
        public int CriticalHours { get; }
        public IReadOnlyCollection<EnrollmentStatus> TrackedStatuses => _trackedStatuses;

        public bool Tracks(EnrollmentStatus status) => _trackedStatuses.Contains(status);

        public string DescribeThresholds()
        {
            return $"warning >= {WarningHours}h, critical >= {CriticalHours}h";
        }
    }

    internal sealed class EnrollmentQueueSlaEvaluation
    {
        public EnrollmentQueueSlaEvaluation(EnrollmentQueueSlaSeverity severity, string label, string ageDisplay, bool tracked)
        {
            Severity = severity;
            Label = label;
            AgeDisplay = ageDisplay;
            Tracked = tracked;
        }

        public EnrollmentQueueSlaSeverity Severity { get; }
        public string Label { get; }
        public string AgeDisplay { get; }
        public bool Tracked { get; }
    }

    internal sealed class EnrollmentQueueSlaService
    {
        private const string WarningHoursKey = "EnrollmentQueueSlaWarningHours";
        private const string CriticalHoursKey = "EnrollmentQueueSlaCriticalHours";
        private const string TrackedStatusesKey = "EnrollmentQueueSlaTrackedStatuses";

        public EnrollmentQueueSlaPolicy LoadPolicy()
        {
            var warningHours = ParseThreshold(WarningHoursKey, EnrollmentQueueSlaPolicy.Default.WarningHours);
            var criticalHours = ParseThreshold(CriticalHoursKey, EnrollmentQueueSlaPolicy.Default.CriticalHours);
            var trackedStatuses = ParseTrackedStatuses(ConfigurationManager.AppSettings[TrackedStatusesKey]);
            return new EnrollmentQueueSlaPolicy(warningHours, criticalHours, trackedStatuses);
        }

        public EnrollmentQueueSlaEvaluation Evaluate(Enrollment? enrollment, DateTime referenceUtc, EnrollmentQueueSlaPolicy policy)
        {
            if (enrollment == null)
            {
                return new EnrollmentQueueSlaEvaluation(EnrollmentQueueSlaSeverity.None, "N/A", "-", tracked: false);
            }

            if (!policy.Tracks(enrollment.Status))
            {
                return new EnrollmentQueueSlaEvaluation(EnrollmentQueueSlaSeverity.None, "N/A", "-", tracked: false);
            }

            var anchor = ResolveAgingAnchor(enrollment);
            if (anchor > referenceUtc)
            {
                anchor = referenceUtc;
            }

            var age = referenceUtc - anchor;
            var ageHours = Math.Max(0, (int)Math.Floor(age.TotalHours));
            var severity = ageHours >= policy.CriticalHours
                ? EnrollmentQueueSlaSeverity.Critical
                : ageHours >= policy.WarningHours
                    ? EnrollmentQueueSlaSeverity.Warning
                    : EnrollmentQueueSlaSeverity.OnTrack;
            var label = severity switch
            {
                EnrollmentQueueSlaSeverity.Critical => "CRITICAL",
                EnrollmentQueueSlaSeverity.Warning => "WARNING",
                _ => "ON_TRACK"
            };

            return new EnrollmentQueueSlaEvaluation(severity, label, FormatAge(age), tracked: true);
        }

        private static DateTime ResolveAgingAnchor(Enrollment enrollment)
        {
            if (enrollment.UpdatedAt != default)
            {
                return enrollment.UpdatedAt;
            }

            if (enrollment.EnrolledAt != default)
            {
                return enrollment.EnrolledAt;
            }

            if (enrollment.CreatedAt != default)
            {
                return enrollment.CreatedAt;
            }

            return DateTime.UtcNow;
        }

        private static string FormatAge(TimeSpan age)
        {
            if (age.TotalHours < 1)
            {
                return "<1h";
            }

            var totalHours = Math.Max(0, (int)Math.Floor(age.TotalHours));
            if (totalHours < 24)
            {
                return $"{totalHours}h";
            }

            var days = totalHours / 24;
            var hours = totalHours % 24;
            return hours == 0 ? $"{days}d" : $"{days}d {hours}h";
        }

        private static int ParseThreshold(string key, int fallback)
        {
            var raw = ConfigurationManager.AppSettings[key];
            return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
        }

        private static IReadOnlyCollection<EnrollmentStatus> ParseTrackedStatuses(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return EnrollmentQueueSlaPolicy.Default.TrackedStatuses;
            }

            var statuses = raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => Enum.TryParse<EnrollmentStatus>(x, true, out _))
                .Select(x => Enum.Parse<EnrollmentStatus>(x, true))
                .Distinct()
                .ToList();
            return statuses.Count == 0 ? EnrollmentQueueSlaPolicy.Default.TrackedStatuses : statuses;
        }
    }
}

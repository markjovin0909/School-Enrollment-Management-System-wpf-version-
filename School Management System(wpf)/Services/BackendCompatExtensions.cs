using System;
using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal static class AccountSecurityServiceCompatExtensions
    {
        public static void LogLogout(this AccountSecurityService service, User? user, string? reason)
        {
            if (user == null)
            {
                return;
            }

            AuditTrailService.Log("LOGOUT", "users", user.Id, null, new
            {
                user.Username,
                user.Role,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Session end" : reason
            });
        }
    }

    internal static class SchoolYearServiceCompatExtensions
    {
        public static SchoolYear? GetActiveSchoolYear(this SchoolYearService service)
        {
            return service
                .GetAll()
                .OrderByDescending(x => x.Id)
                .FirstOrDefault(x => x.Status == SchoolYearStatus.ACTIVE);
        }

        public static void Restore(this SchoolYearService service, long id)
        {
            var entity = service.GetById(id);
            if (entity == null)
            {
                return;
            }

            entity.IsArchived = false;
            entity.UpdatedAt = DateTime.UtcNow;
            service.Update(entity);
        }
    }

    internal static class SectionServiceCompatExtensions
    {
        public static void Restore(this SectionService service, long id)
        {
            var entity = service.GetById(id);
            if (entity == null)
            {
                return;
            }

            entity.IsArchived = false;
            entity.UpdatedAt = DateTime.UtcNow;
            service.Update(entity);
        }
    }

    internal static class SchoolSettingServiceCompatExtensions
    {
        private static readonly object CounterLock = new();
        private static int _fallbackCounter = 1;

        public static string ReserveNextStudentNumber(this SchoolSettingService service)
        {
            lock (CounterLock)
            {
                var latest = service.GetAll().OrderByDescending(x => x.Id).FirstOrDefault();
                if (latest != null && latest.NextStudentNumber > _fallbackCounter)
                {
                    _fallbackCounter = latest.NextStudentNumber;
                }

                var prefix = ResolvePrefix(latest);
                var value = _fallbackCounter++;

                if (latest != null)
                {
                    latest.NextStudentNumber = value + 1;
                    latest.UpdatedAt = DateTime.UtcNow;
                    service.Update(latest);
                }

                return $"{prefix}-{value:000000}";
            }
        }

        private static string ResolvePrefix(SchoolSetting? setting)
        {
            if (!string.IsNullOrWhiteSpace(setting?.StudentNumberPrefix))
            {
                return setting.StudentNumberPrefix.Trim().ToUpperInvariant();
            }

            var schoolCode = setting?.SchoolCode ?? string.Empty;
            var cleaned = new string(schoolCode.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                return "S";
            }

            return cleaned.Length <= 3 ? cleaned : cleaned.Substring(0, 3);
        }
    }

    internal static class SchoolSettingCompat
    {
        public static List<long> ParseGradeLevelIds(string? rawIds)
        {
            if (string.IsNullOrWhiteSpace(rawIds))
            {
                return new List<long>();
            }

            return rawIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => long.TryParse(x, out _))
                .Select(long.Parse)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }

        public static string? NormalizeGradeLevelIds(IEnumerable<long>? ids)
        {
            if (ids == null)
            {
                return null;
            }

            var normalized = ids
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            return normalized.Count == 0 ? null : string.Join(",", normalized);
        }
    }

    internal static class EnrollmentServiceCompatExtensions
    {
        public static OperationResult<EnrollmentValidationSummary> BuildValidationSummary(
            this EnrollmentService service,
            EnrollmentDraft draft,
            long? existingEnrollmentId = null)
        {
            return service.BuildValidationSummary(draft, existingEnrollmentId);
        }

        public static OperationResult<Enrollment> SubmitEnrollmentRequest(
            this EnrollmentService service,
            EnrollmentDraft draft,
            long? existingEnrollmentId = null)
        {
            return service.SubmitEnrollmentRequest(draft, existingEnrollmentId);
        }

        public static OperationResult<Enrollment> ApproveEnrollment(this EnrollmentService service, long enrollmentId)
        {
            return service.ApproveEnrollment(enrollmentId);
        }

        public static OperationResult<Enrollment> ReturnForCorrection(this EnrollmentService service, long enrollmentId)
        {
            return service.ReturnForCorrection(enrollmentId);
        }

        public static OperationResult<Enrollment> SetStatus(this EnrollmentService service, long enrollmentId, EnrollmentStatus status)
        {
            return service.SetStatus(enrollmentId, status);
        }

        public static OperationResult<int> PromoteWaitlist(this EnrollmentService service, long schoolYearId, long sectionId)
        {
            return service.PromoteWaitlist(schoolYearId, sectionId);
        }
    }
}

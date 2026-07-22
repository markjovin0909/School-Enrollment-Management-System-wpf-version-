using System;
using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class SchoolSettingService
    {
        public IEnumerable<SchoolSetting> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new SchoolSettingRepository(db);
            return repo.GetAll();
        }

        public SchoolSetting? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new SchoolSettingRepository(db);
            return repo.GetById(id);
        }

        public void Create(SchoolSetting entity)
        {
            using var db = new AppDbContext();
            var repo = new SchoolSettingRepository(db);
            repo.Add(entity);
        }

        public void Update(SchoolSetting entity)
        {
            using var db = new AppDbContext();
            var repo = new SchoolSettingRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new SchoolSettingRepository(db);
            repo.Delete(id);
        }

        /// <summary>
        /// Latest school settings row, or null when none has been saved yet.
        /// </summary>
        public SchoolSetting? GetLatest()
        {
            using var db = new AppDbContext();
            return db.SchoolSettings.OrderByDescending(x => x.Id).FirstOrDefault();
        }

        public SchoolSetting GetOrCreateDefault()
        {
            using var db = new AppDbContext();
            var latest = db.SchoolSettings.OrderByDescending(x => x.Id).FirstOrDefault();
            if (latest != null)
            {
                return latest;
            }

            var now = DateTime.UtcNow;
            var setting = new SchoolSetting
            {
                SchoolName = AppBrandingDefaults.AppName,
                SchoolCode = AppBrandingDefaults.SchoolCode,
                SchoolAddress = string.Empty,
                PrincipalName = string.Empty,
                GradingSetup = "K-12 quarter system",
                EnrollmentConfiguration = "Open",
                PrintHeaderLine1 = AppBrandingDefaults.PrintHeaderLine1,
                PrintHeaderLine2 = AppBrandingDefaults.PrintHeaderLine2,
                SchoolLogoPath = null,
                StudentNumberPrefix = AppBrandingDefaults.StudentNumberPrefix,
                NextStudentNumber = 1,
                DefaultSectionCapacity = 45,
                DefaultGradeLevelIds = null,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.SchoolSettings.Add(setting);
            db.SaveChanges();
            return setting;
        }

        public int GetDefaultSectionCapacity()
        {
            var setting = GetLatest();
            return setting != null && setting.DefaultSectionCapacity > 0
                ? setting.DefaultSectionCapacity
                : 45;
        }

        public string GetEnrollmentConfiguration()
        {
            var setting = GetLatest();
            if (setting == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(setting.EnrollmentConfiguration))
            {
                return setting.EnrollmentConfiguration.Trim();
            }

            return string.IsNullOrWhiteSpace(setting.GradingSetup)
                ? string.Empty
                : setting.GradingSetup.Trim();
        }

        public IReadOnlyList<long> GetDefaultGradeLevelIds()
        {
            using var db = new AppDbContext();
            var raw = db.SchoolSettings
                .OrderByDescending(x => x.Id)
                .Select(x => x.DefaultGradeLevelIds)
                .FirstOrDefault();

            return ParseGradeLevelIds(raw);
        }

        public long? GetPrimaryDefaultGradeLevelId()
        {
            var ids = GetDefaultGradeLevelIds();
            return ids.Count > 0 ? ids[0] : null;
        }

        /// <summary>
        /// Orders grade levels so configured default-scope grades appear first.
        /// </summary>
        public List<GradeLevel> OrderGradeLevelsByDefaultScope(IEnumerable<GradeLevel> gradeLevels)
        {
            var defaults = GetDefaultGradeLevelIds().ToHashSet();
            return gradeLevels
                .OrderBy(g => defaults.Contains(g.Id) ? 0 : 1)
                .ThenBy(g => g.Code ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.Id)
                .ToList();
        }

        public SchoolPrintIdentity GetPrintIdentity()
        {
            var setting = GetLatest();
            var schoolName = AppBrandingDefaults.ResolveSchoolName(setting?.SchoolName);
            var schoolCode = string.IsNullOrWhiteSpace(setting?.SchoolCode)
                ? AppBrandingDefaults.SchoolCode
                : setting!.SchoolCode.Trim();
            var schoolAddress = setting?.SchoolAddress?.Trim() ?? string.Empty;
            var principalName = setting?.PrincipalName?.Trim() ?? string.Empty;
            var printHeader1 = string.IsNullOrWhiteSpace(setting?.PrintHeaderLine1)
                || AppBrandingDefaults.IsUnsetOrLegacySchoolName(setting?.PrintHeaderLine1)
                ? AppBrandingDefaults.PrintHeaderLine1
                : setting!.PrintHeaderLine1!.Trim();
            var printHeader2 = string.IsNullOrWhiteSpace(setting?.PrintHeaderLine2)
                ? AppBrandingDefaults.PrintHeaderLine2
                : setting!.PrintHeaderLine2!.Trim();

            return new SchoolPrintIdentity(
                schoolName,
                schoolCode,
                schoolAddress,
                principalName,
                printHeader1,
                printHeader2);
        }

        /// <summary>
        /// Upgrades legacy product naming to the official eTinun-an branding when settings still use old defaults.
        /// </summary>
        public void EnsureProductBrandingDefaults()
        {
            try
            {
                using var db = new AppDbContext();
                var setting = db.SchoolSettings.OrderByDescending(x => x.Id).FirstOrDefault();
                if (setting == null)
                {
                    GetOrCreateDefault();
                    return;
                }

                var changed = false;
                if (AppBrandingDefaults.IsUnsetOrLegacySchoolName(setting.SchoolName))
                {
                    setting.SchoolName = AppBrandingDefaults.AppName;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(setting.SchoolCode)
                    || string.Equals(setting.SchoolCode.Trim(), "SMS-001", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(setting.SchoolCode.Trim(), "SMS", StringComparison.OrdinalIgnoreCase))
                {
                    setting.SchoolCode = AppBrandingDefaults.SchoolCode;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(setting.PrintHeaderLine1)
                    || AppBrandingDefaults.IsUnsetOrLegacySchoolName(setting.PrintHeaderLine1))
                {
                    setting.PrintHeaderLine1 = AppBrandingDefaults.PrintHeaderLine1;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(setting.PrintHeaderLine2)
                    || string.Equals(setting.PrintHeaderLine2.Trim(), "Enrollment Services Office", StringComparison.OrdinalIgnoreCase))
                {
                    setting.PrintHeaderLine2 = AppBrandingDefaults.PrintHeaderLine2;
                    changed = true;
                }

                if (string.IsNullOrWhiteSpace(setting.StudentNumberPrefix)
                    || string.Equals(setting.StudentNumberPrefix.Trim(), "S", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(setting.StudentNumberPrefix.Trim(), "SMS", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(setting.StudentNumberPrefix.Trim(), "STU", StringComparison.OrdinalIgnoreCase))
                {
                    setting.StudentNumberPrefix = AppBrandingDefaults.StudentNumberPrefix;
                    changed = true;
                }

                if (changed)
                {
                    setting.UpdatedAt = DateTime.UtcNow;
                    db.SaveChanges();
                }
            }
            catch
            {
                // Branding upgrade must never block startup.
            }
        }

        public static IReadOnlyList<long> ParseGradeLevelIds(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return new List<long>();
            }

            return rawValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => long.TryParse(x, out _))
                .Select(long.Parse)
                .Distinct()
                .ToList();
        }

        public static string NormalizeGradeLevelIds(IEnumerable<long> gradeLevelIds)
        {
            return string.Join(",", gradeLevelIds
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x));
        }

        /// <summary>
        /// Normalizes a student-number prefix. Empty input falls back to school code, then existing, then "S".
        /// </summary>
        public static string NormalizeStudentNumberPrefix(string? requestedPrefix, string? schoolCode, string? existingPrefix = null)
        {
            var cleanedRequested = CleanPrefixToken(requestedPrefix);
            if (!string.IsNullOrWhiteSpace(cleanedRequested))
            {
                return cleanedRequested;
            }

            var cleanedCode = CleanPrefixToken(schoolCode);
            if (!string.IsNullOrWhiteSpace(cleanedCode))
            {
                return cleanedCode.Length <= 3 ? cleanedCode : cleanedCode.Substring(0, 3);
            }

            var cleanedExisting = CleanPrefixToken(existingPrefix);
            if (!string.IsNullOrWhiteSpace(cleanedExisting))
            {
                return cleanedExisting;
            }

            return "S";
        }

        private static string CleanPrefixToken(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var cleaned = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (cleaned.Length > 12)
            {
                cleaned = cleaned.Substring(0, 12);
            }

            return cleaned;
        }

        public string ReserveNextStudentNumber()
        {
            using var db = new AppDbContext();
            var setting = db.SchoolSettings.OrderByDescending(x => x.Id).FirstOrDefault();
            if (setting == null)
            {
                setting = GetOrCreateDefault();
            }

            var prefix = NormalizeStudentNumberPrefix(setting.StudentNumberPrefix, setting.SchoolCode);
            var number = setting.NextStudentNumber <= 0 ? 1 : setting.NextStudentNumber;
            var studentNumber = $"{prefix}-{number:000000}";

            setting.StudentNumberPrefix = prefix;
            setting.NextStudentNumber = number + 1;
            setting.UpdatedAt = DateTime.UtcNow;
            db.SchoolSettings.Update(setting);
            db.SaveChanges();

            return studentNumber;
        }

        public string PeekNextStudentNumber()
        {
            using var db = new AppDbContext();
            var setting = db.SchoolSettings.OrderByDescending(x => x.Id).FirstOrDefault();
            if (setting == null)
            {
                setting = GetOrCreateDefault();
            }

            var prefix = NormalizeStudentNumberPrefix(setting.StudentNumberPrefix, setting.SchoolCode);
            var number = setting.NextStudentNumber <= 0 ? 1 : setting.NextStudentNumber;
            return $"{prefix}-{number:000000}";
        }
    }

    internal sealed record SchoolPrintIdentity(
        string SchoolName,
        string SchoolCode,
        string SchoolAddress,
        string PrincipalName,
        string PrintHeaderLine1,
        string PrintHeaderLine2);
}

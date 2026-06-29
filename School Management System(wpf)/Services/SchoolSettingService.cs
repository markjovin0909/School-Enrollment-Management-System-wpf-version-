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

        public SchoolSetting GetOrCreateDefault()
        {
            using var db = new AppDbContext();
            var latest = db.SchoolSettings.OrderByDescending(x => x.Id).FirstOrDefault();
            if (latest != null)
            {
                return latest;
            }

            var now = System.DateTime.UtcNow;
            var setting = new SchoolSetting
            {
                SchoolName = "School Management System",
                SchoolCode = "SMS-001",
                SchoolAddress = string.Empty,
                PrincipalName = string.Empty,
                GradingSetup = "K-12 quarter system",
                EnrollmentConfiguration = "Open",
                StudentNumberPrefix = "S",
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

        public IReadOnlyList<long> GetDefaultGradeLevelIds()
        {
            using var db = new AppDbContext();
            var raw = db.SchoolSettings
                .OrderByDescending(x => x.Id)
                .Select(x => x.DefaultGradeLevelIds)
                .FirstOrDefault();

            return ParseGradeLevelIds(raw);
        }

        public static IReadOnlyList<long> ParseGradeLevelIds(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return new List<long>();
            }

            return rawValue
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries)
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

        public string ReserveNextStudentNumber()
        {
            using var db = new AppDbContext();
            var setting = db.SchoolSettings.OrderByDescending(x => x.Id).FirstOrDefault();
            if (setting == null)
            {
                setting = GetOrCreateDefault();
            }

            var prefix = string.IsNullOrWhiteSpace(setting.StudentNumberPrefix)
                ? "S"
                : setting.StudentNumberPrefix.Trim().ToUpperInvariant();
            var number = setting.NextStudentNumber <= 0 ? 1 : setting.NextStudentNumber;
            var studentNumber = $"{prefix}-{number:000000}";

            setting.NextStudentNumber = number + 1;
            setting.UpdatedAt = System.DateTime.UtcNow;
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

            var prefix = string.IsNullOrWhiteSpace(setting.StudentNumberPrefix)
                ? "S"
                : setting.StudentNumberPrefix.Trim().ToUpperInvariant();
            var number = setting.NextStudentNumber <= 0 ? 1 : setting.NextStudentNumber;
            return $"{prefix}-{number:000000}";
        }
    }
}

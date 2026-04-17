using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class SchoolYearService : ISchoolYearService
    {
        public IEnumerable<SchoolYear> GetAll()
        {
            using var db = new AppDbContext();
            return db.SchoolYears
                .OrderBy(x => x.IsArchived)
                .ThenByDescending(x => x.StartDate)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        public SchoolYear? GetById(long id)
        {
            using var db = new AppDbContext();
            return db.SchoolYears.Find(id);
        }

        public SchoolYear? GetActiveSchoolYear()
        {
            using var db = new AppDbContext();
            return db.SchoolYears
                .Where(x => x.Status == SchoolYearStatus.ACTIVE && !x.IsArchived)
                .OrderByDescending(x => x.StartDate)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();
        }

        public void Create(SchoolYear entity)
        {
            using var db = new AppDbContext();
            ValidateSchoolYear(entity);

            var now = DateTime.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            entity.UpdatedAt = now;

            ApplySingleActiveRule(db, entity.Id, entity.Status, now);
            db.SchoolYears.Add(entity);
            db.SaveChanges();
        }

        public void Update(SchoolYear entity)
        {
            using var db = new AppDbContext();
            ValidateSchoolYear(entity);

            var existing = db.SchoolYears.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("School year record was not found.");
            }

            existing.Name = entity.Name.Trim();
            existing.StartDate = entity.StartDate?.Date;
            existing.EndDate = entity.EndDate?.Date;
            existing.EnrollmentOpenDate = entity.EnrollmentOpenDate?.Date;
            existing.EnrollmentCloseDate = entity.EnrollmentCloseDate?.Date;
            existing.Status = entity.Status;
            existing.IsArchived = entity.IsArchived;
            existing.UpdatedAt = DateTime.UtcNow;

            ApplySingleActiveRule(db, existing.Id, existing.Status, existing.UpdatedAt);
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var existing = db.SchoolYears.Find(id);
            if (existing == null)
            {
                return;
            }

            if (existing.Status == SchoolYearStatus.ACTIVE)
            {
                throw new DomainValidationException("Active school year cannot be archived. Set it to CLOSED first.");
            }

            if (existing.IsArchived)
            {
                return;
            }

            var repo = new SchoolYearRepository(db);
            repo.Delete(id);
        }

        public void Restore(long id)
        {
            using var db = new AppDbContext();
            var existing = db.SchoolYears.Find(id);
            if (existing == null)
            {
                throw new DomainValidationException("School year record was not found.");
            }

            existing.IsArchived = false;
            if (existing.Status == SchoolYearStatus.ACTIVE)
            {
                ApplySingleActiveRule(db, existing.Id, existing.Status, DateTime.UtcNow);
            }

            existing.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();
        }

        public OperationResult ValidateEnrollmentWindow(long schoolYearId, DateTime? referenceDate = null)
        {
            using var db = new AppDbContext();
            var schoolYear = db.SchoolYears.Find(schoolYearId);
            if (schoolYear == null)
            {
                return OperationResult.Fail("Selected school year was not found.");
            }

            if (schoolYear.IsArchived)
            {
                return OperationResult.Fail($"Enrollment is closed for {schoolYear.Name} because the school year is archived.");
            }

            if (schoolYear.Status != SchoolYearStatus.ACTIVE)
            {
                return OperationResult.Fail($"Enrollment is closed for {schoolYear.Name} because its status is {schoolYear.Status}.");
            }

            var setting = db.SchoolSettings
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();
            if (setting == null && !schoolYear.EnrollmentOpenDate.HasValue && !schoolYear.EnrollmentCloseDate.HasValue)
            {
                return OperationResult.Ok();
            }

            var date = (referenceDate ?? DateTime.Today).Date;
            var openDate = schoolYear.EnrollmentOpenDate?.Date ?? setting?.EnrollmentOpenDate?.Date;
            var closeDate = schoolYear.EnrollmentCloseDate?.Date ?? setting?.EnrollmentCloseDate?.Date;

            if (openDate.HasValue && closeDate.HasValue)
            {
                var open = openDate.Value;
                var close = closeDate.Value;
                if (open > close)
                {
                    return OperationResult.Fail("Enrollment period configuration is invalid: open date is later than close date.");
                }

                if (date < open || date > close)
                {
                    return OperationResult.Fail(
                        $"Enrollment period is closed ({open.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} to {close.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}).");
                }
            }
            else if (openDate.HasValue)
            {
                var open = openDate.Value;
                if (date < open)
                {
                    return OperationResult.Fail($"Enrollment opens on {open.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.");
                }
            }
            else if (closeDate.HasValue)
            {
                var close = closeDate.Value;
                if (date > close)
                {
                    return OperationResult.Fail($"Enrollment closed on {close.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.");
                }
            }

            return OperationResult.Ok();
        }

        private static void ValidateSchoolYear(SchoolYear entity)
        {
            if (entity == null)
            {
                throw new DomainValidationException("School year data is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                throw new DomainValidationException("School year name is required.");
            }

            if (entity.StartDate.HasValue && entity.EndDate.HasValue && entity.StartDate.Value.Date > entity.EndDate.Value.Date)
            {
                throw new DomainValidationException("School year start date cannot be later than end date.");
            }

            if (entity.EnrollmentOpenDate.HasValue && entity.EnrollmentCloseDate.HasValue && entity.EnrollmentOpenDate.Value.Date > entity.EnrollmentCloseDate.Value.Date)
            {
                throw new DomainValidationException("Enrollment open date cannot be later than enrollment close date.");
            }

            if (entity.IsArchived && entity.Status == SchoolYearStatus.ACTIVE)
            {
                throw new DomainValidationException("Archived school year cannot be ACTIVE.");
            }
        }

        private static void ApplySingleActiveRule(AppDbContext db, long currentSchoolYearId, SchoolYearStatus status, DateTime timestampUtc)
        {
            if (status != SchoolYearStatus.ACTIVE)
            {
                return;
            }

            var activeOthers = db.SchoolYears
                .Where(x => x.Status == SchoolYearStatus.ACTIVE && !x.IsArchived && x.Id != currentSchoolYearId)
                .ToList();
            foreach (var schoolYear in activeOthers)
            {
                schoolYear.Status = SchoolYearStatus.CLOSED;
                schoolYear.UpdatedAt = timestampUtc;
            }
        }
    }
}

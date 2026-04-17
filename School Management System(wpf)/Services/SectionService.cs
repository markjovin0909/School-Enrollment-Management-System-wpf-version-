using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class SectionService : ISectionService
    {
        public IEnumerable<Section> GetAll()
        {
            using var db = new AppDbContext();
            return db.Sections
                .OrderBy(x => x.IsArchived)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Id)
                .ToList();
        }

        public Section? GetById(long id)
        {
            using var db = new AppDbContext();
            return db.Sections.Find(id);
        }

        public void Create(Section entity)
        {
            using var db = new AppDbContext();
            ValidateSection(db, entity, null);

            var now = System.DateTime.UtcNow;
            if (!entity.Capacity.HasValue || entity.Capacity.Value <= 0)
            {
                var defaultCapacity = db.SchoolSettings
                    .OrderByDescending(x => x.Id)
                    .Select(x => x.DefaultSectionCapacity)
                    .FirstOrDefault();
                entity.Capacity = defaultCapacity > 0 ? defaultCapacity : 45;
            }

            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            entity.UpdatedAt = now;

            db.Sections.Add(entity);
            db.SaveChanges();
        }

        public void Update(Section entity)
        {
            using var db = new AppDbContext();
            var existing = db.Sections.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("Section record was not found.");
            }

            ValidateSection(db, entity, existing.Id);

            existing.Name = entity.Name.Trim();
            existing.SchoolYearId = entity.SchoolYearId;
            existing.GradeLevelId = entity.GradeLevelId;
            existing.Capacity = entity.Capacity;
            existing.AdviserTeacherId = entity.AdviserTeacherId;
            existing.IsArchived = entity.IsArchived;
            existing.UpdatedAt = System.DateTime.UtcNow;
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var existing = db.Sections.Find(id);
            if (existing == null)
            {
                return;
            }

            var schoolYear = db.SchoolYears.Find(existing.SchoolYearId);
            var schoolYearIsActive = schoolYear != null && schoolYear.Status == SchoolYearStatus.ACTIVE && !schoolYear.IsArchived;
            var hasActiveEnrollments = db.Enrollments.Any(x =>
                x.SectionId == id &&
                (x.Status == EnrollmentStatus.PENDING ||
                 x.Status == EnrollmentStatus.ENROLLED ||
                 x.Status == EnrollmentStatus.RESERVED));
            var hasActiveOfferings = db.ClassOfferings.Any(x =>
                x.SectionId == id &&
                x.Status != ClassOfferingStatus.ARCHIVED);
            if (schoolYearIsActive && (hasActiveEnrollments || hasActiveOfferings))
            {
                throw new DomainValidationException("Section cannot be archived while it still has active enrollments or class offerings in an active school year.");
            }

            if (existing.IsArchived)
            {
                return;
            }

            var repo = new SectionRepository(db);
            repo.Delete(id);
        }

        public void Restore(long id)
        {
            using var db = new AppDbContext();
            var existing = db.Sections.Find(id);
            if (existing == null)
            {
                throw new DomainValidationException("Section record was not found.");
            }

            var schoolYear = db.SchoolYears.Find(existing.SchoolYearId);
            if (schoolYear == null)
            {
                throw new DomainValidationException("Section cannot be restored because its school year does not exist.");
            }

            if (schoolYear.IsArchived)
            {
                throw new DomainValidationException("Section cannot be restored while its school year is archived.");
            }

            existing.IsArchived = false;
            existing.UpdatedAt = System.DateTime.UtcNow;
            ValidateSection(db, existing, existing.Id);
            db.SaveChanges();
        }

        private static void ValidateSection(AppDbContext db, Section entity, long? excludeId)
        {
            if (entity == null)
            {
                throw new DomainValidationException("Section data is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                throw new DomainValidationException("Section name is required.");
            }

            if (entity.SchoolYearId <= 0)
            {
                throw new DomainValidationException("School year is required for section.");
            }

            if (entity.GradeLevelId <= 0)
            {
                throw new DomainValidationException("Grade level is required for section.");
            }

            var schoolYearExists = db.SchoolYears.Any(x => x.Id == entity.SchoolYearId);
            if (!schoolYearExists)
            {
                throw new DomainValidationException("Selected school year does not exist.");
            }

            var schoolYear = db.SchoolYears.Find(entity.SchoolYearId);
            if (schoolYear != null && schoolYear.IsArchived)
            {
                throw new DomainValidationException("Archived school year cannot be used for section management.");
            }

            var gradeLevelExists = db.GradeLevels.Any(x => x.Id == entity.GradeLevelId);
            if (!gradeLevelExists)
            {
                throw new DomainValidationException("Selected grade level does not exist.");
            }

            var normalizedName = entity.Name.Trim();
            var duplicateName = db.Sections
                .AsEnumerable()
                .Any(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    !x.IsArchived &&
                    x.SchoolYearId == entity.SchoolYearId &&
                    x.GradeLevelId == entity.GradeLevelId &&
                    string.Equals(x.Name?.Trim(), normalizedName, System.StringComparison.OrdinalIgnoreCase));
            if (duplicateName)
            {
                throw new DomainValidationException("Section name already exists for the selected school year and grade level.");
            }

            if (entity.Capacity.HasValue && entity.Capacity.Value <= 0)
            {
                throw new DomainValidationException("Section capacity must be greater than zero.");
            }

            if (entity.AdviserTeacherId.HasValue)
            {
                var adviser = db.Teachers.Find(entity.AdviserTeacherId.Value);
                if (adviser == null)
                {
                    throw new DomainValidationException("Selected adviser does not exist.");
                }

                if (adviser.Status != UserStatus.ACTIVE)
                {
                    throw new DomainValidationException("Selected adviser must be active.");
                }

                var conflictingAdvisory = db.Sections.Any(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    !x.IsArchived &&
                    x.SchoolYearId == entity.SchoolYearId &&
                    x.AdviserTeacherId == entity.AdviserTeacherId.Value);
                if (conflictingAdvisory)
                {
                    throw new DomainValidationException("Selected adviser is already assigned to another section in the same school year.");
                }
            }
        }
    }
}

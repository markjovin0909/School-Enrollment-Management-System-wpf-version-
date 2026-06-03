using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal class GradeLevelService : IGradeLevelService
    {
        public IEnumerable<GradeLevel> GetAll()
        {
            using var db = new AppDbContext();
            return db.GradeLevels
                .OrderBy(x => x.Code)
                .ThenBy(x => x.Name)
                .ToList();
        }

        public GradeLevel? GetById(long id)
        {
            using var db = new AppDbContext();
            return db.GradeLevels.Find(id);
        }

        public void Create(GradeLevel entity)
        {
            using var db = new AppDbContext();
            ValidateGradeLevel(db, entity, null);

            var now = System.DateTime.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            entity.UpdatedAt = now;

            db.GradeLevels.Add(entity);
            db.SaveChanges();
        }

        public void Update(GradeLevel entity)
        {
            using var db = new AppDbContext();
            var existing = db.GradeLevels.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("Grade level record was not found.");
            }

            ValidateGradeLevel(db, entity, existing.Id);

            existing.Code = entity.Code.Trim();
            existing.Name = entity.Name.Trim();
            existing.UpdatedAt = System.DateTime.UtcNow;
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var existing = db.GradeLevels.Find(id);
            if (existing == null)
            {
                return;
            }

            var isReferenced = db.Students.Any(x => x.PreferredGradeLevelId == id)
                || db.Sections.Any(x => x.GradeLevelId == id)
                || db.Enrollments.Any(x => x.GradeLevelId == id)
                || db.Subjects.Any(x => x.GradeLevelId == id)
                || db.CurriculumSubjects.Any(x => x.GradeLevelId == id);
            if (isReferenced)
            {
                throw new DomainValidationException("Grade level cannot be deleted because it is already linked to existing records.");
            }

            db.GradeLevels.Remove(existing);
            db.SaveChanges();
        }

        private static void ValidateGradeLevel(AppDbContext db, GradeLevel entity, long? excludeId)
        {
            if (entity == null)
            {
                throw new DomainValidationException("Grade level data is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.Code))
            {
                throw new DomainValidationException("Grade level code is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                throw new DomainValidationException("Grade level name is required.");
            }

            var code = entity.Code.Trim().ToLower();
            var name = entity.Name.Trim().ToLower();

            var duplicateCode = db.GradeLevels
                .Any(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    x.Code != null && x.Code.ToLower() == code);
            if (duplicateCode)
            {
                throw new DomainValidationException("Grade level code already exists.");
            }

            var duplicateName = db.GradeLevels
                .Any(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    x.Name != null && x.Name.ToLower() == name);
            if (duplicateName)
            {
                throw new DomainValidationException("Grade level name already exists.");
            }
        }
    }
}

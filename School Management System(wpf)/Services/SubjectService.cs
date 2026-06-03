using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal class SubjectService : ISubjectService
    {
        public IEnumerable<Subject> GetAll()
        {
            using var db = new AppDbContext();
            return db.Subjects
                .OrderBy(x => x.Code)
                .ToList();
        }

        public Subject? GetById(long id)
        {
            using var db = new AppDbContext();
            return db.Subjects.Find(id);
        }

        public void Create(Subject entity)
        {
            using var db = new AppDbContext();
            ValidateSubject(db, entity, null);

            var now = System.DateTime.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            entity.UpdatedAt = now;

            db.Subjects.Add(entity);
            db.SaveChanges();
        }

        public void Update(Subject entity)
        {
            using var db = new AppDbContext();
            var existing = db.Subjects.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("Subject record was not found.");
            }

            ValidateSubject(db, entity, existing.Id);

            existing.Code = entity.Code.Trim();
            existing.Title = entity.Title.Trim();
            existing.Description = string.IsNullOrWhiteSpace(entity.Description) ? null : entity.Description.Trim();
            existing.GradeLevelId = entity.GradeLevelId;
            existing.IsActive = entity.IsActive;
            existing.UpdatedAt = System.DateTime.UtcNow;
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var existing = db.Subjects.Find(id);
            if (existing == null)
            {
                return;
            }

            existing.IsActive = false;
            existing.UpdatedAt = System.DateTime.UtcNow;
            db.SaveChanges();
        }

        private static void ValidateSubject(AppDbContext db, Subject entity, long? excludeId)
        {
            if (entity == null)
            {
                throw new DomainValidationException("Subject data is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.Code))
            {
                throw new DomainValidationException("Subject code is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.Title))
            {
                throw new DomainValidationException("Subject title is required.");
            }

            var normalizedCode = entity.Code.Trim().ToLower();
            var duplicateCode = db.Subjects
                .Any(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    x.Code != null && x.Code.ToLower() == normalizedCode);
            if (duplicateCode)
            {
                throw new DomainValidationException("Subject code already exists.");
            }
        }
    }
}

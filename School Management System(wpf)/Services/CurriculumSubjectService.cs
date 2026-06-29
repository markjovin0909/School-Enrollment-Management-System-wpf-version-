using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class CurriculumSubjectService
    {
        public IEnumerable<CurriculumSubject> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new CurriculumSubjectRepository(db);
            return repo.GetAll();
        }

        public CurriculumSubject? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new CurriculumSubjectRepository(db);
            return repo.GetById(id);
        }

        public void Create(CurriculumSubject entity)
        {
            using var db = new AppDbContext();
            ValidateMapping(db, entity, null);

            var now = System.DateTime.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            entity.UpdatedAt = now;

            db.CurriculumSubjects.Add(entity);
            db.SaveChanges();
        }

        public void Update(CurriculumSubject entity)
        {
            using var db = new AppDbContext();
            var existing = db.CurriculumSubjects.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("Curriculum mapping was not found.");
            }

            ValidateMapping(db, entity, existing.Id);

            existing.CurriculumId = entity.CurriculumId;
            existing.GradeLevelId = entity.GradeLevelId;
            existing.SubjectId = entity.SubjectId;
            existing.Semester = entity.Semester;
            existing.IsRequired = entity.IsRequired;
            existing.SortOrder = entity.SortOrder;
            existing.UpdatedAt = System.DateTime.UtcNow;
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new CurriculumSubjectRepository(db);
            repo.Delete(id);
        }

        private static void ValidateMapping(AppDbContext db, CurriculumSubject entity, long? excludeId)
        {
            if (entity == null)
            {
                throw new DomainValidationException("Curriculum mapping data is required.");
            }

            if (entity.CurriculumId <= 0 || !db.Curricula.Any(x => x.Id == entity.CurriculumId))
            {
                throw new DomainValidationException("Valid curriculum is required.");
            }

            if (entity.GradeLevelId <= 0 || !db.GradeLevels.Any(x => x.Id == entity.GradeLevelId))
            {
                throw new DomainValidationException("Valid grade level is required.");
            }

            var subject = db.Subjects.Find(entity.SubjectId);
            if (subject == null)
            {
                throw new DomainValidationException("Valid subject is required.");
            }

            if (subject.GradeLevelId.HasValue && subject.GradeLevelId.Value != entity.GradeLevelId)
            {
                throw new DomainValidationException("Selected subject is not assigned to the selected grade level.");
            }

            var duplicateMapping = db.CurriculumSubjects.Any(x =>
                (!excludeId.HasValue || x.Id != excludeId.Value) &&
                x.CurriculumId == entity.CurriculumId &&
                x.GradeLevelId == entity.GradeLevelId &&
                x.SubjectId == entity.SubjectId);
            if (duplicateMapping)
            {
                throw new DomainValidationException("This subject is already mapped to the selected curriculum and grade level.");
            }
        }
    }
}

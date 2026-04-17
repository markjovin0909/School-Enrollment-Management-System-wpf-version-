using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class ClassOfferingService : IClassOfferingService
    {
        private const int TeacherLoadLimit = 8;

        public IEnumerable<ClassOffering> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new ClassOfferingRepository(db);
            return repo.GetAll();
        }

        public ClassOffering? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new ClassOfferingRepository(db);
            return repo.GetById(id);
        }

        public void Create(ClassOffering entity)
        {
            using var db = new AppDbContext();
            ValidateOffering(db, entity, null);

            var now = System.DateTime.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            entity.UpdatedAt = now;

            db.ClassOfferings.Add(entity);
            db.SaveChanges();
        }

        public void Update(ClassOffering entity)
        {
            using var db = new AppDbContext();
            var existing = db.ClassOfferings.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("Class offering was not found.");
            }

            ValidateOffering(db, entity, existing.Id);

            existing.SchoolYearId = entity.SchoolYearId;
            existing.SectionId = entity.SectionId;
            existing.SubjectId = entity.SubjectId;
            existing.TeacherId = entity.TeacherId;
            existing.CurriculumId = entity.CurriculumId;
            existing.Status = entity.Status;
            existing.Room = string.IsNullOrWhiteSpace(entity.Room) ? null : entity.Room.Trim();
            existing.UpdatedAt = System.DateTime.UtcNow;
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new ClassOfferingRepository(db);
            repo.Delete(id);
        }

        private static void ValidateOffering(AppDbContext db, ClassOffering entity, long? excludeId)
        {
            if (entity == null)
            {
                throw new DomainValidationException("Class offering data is required.");
            }

            if (entity.SchoolYearId <= 0 || !db.SchoolYears.Any(x => x.Id == entity.SchoolYearId))
            {
                throw new DomainValidationException("Valid school year is required.");
            }

            var schoolYear = db.SchoolYears.Find(entity.SchoolYearId);
            if (schoolYear != null && schoolYear.IsArchived)
            {
                throw new DomainValidationException("Archived school year cannot be used for class offerings.");
            }

            var section = db.Sections.Find(entity.SectionId);
            if (section == null)
            {
                throw new DomainValidationException("Valid section is required.");
            }

            if (section.IsArchived)
            {
                throw new DomainValidationException("Archived section cannot be used for class offerings.");
            }

            if (section.SchoolYearId != entity.SchoolYearId)
            {
                throw new DomainValidationException("Section does not belong to the selected school year.");
            }

            var subject = db.Subjects.Find(entity.SubjectId);
            if (subject == null)
            {
                throw new DomainValidationException("Valid subject is required.");
            }

            if (subject.GradeLevelId.HasValue && subject.GradeLevelId.Value != section.GradeLevelId)
            {
                throw new DomainValidationException("Selected subject is not assigned to the section's grade level.");
            }

            var duplicateOffering = db.ClassOfferings.Any(x =>
                (!excludeId.HasValue || x.Id != excludeId.Value) &&
                x.SchoolYearId == entity.SchoolYearId &&
                x.SectionId == entity.SectionId &&
                x.SubjectId == entity.SubjectId);
            if (duplicateOffering)
            {
                throw new DomainValidationException("Duplicate class offering is not allowed for the same school year, section, and subject.");
            }

            if (entity.CurriculumId.HasValue)
            {
                var curriculumExists = db.Curricula.Any(x => x.Id == entity.CurriculumId.Value);
                if (!curriculumExists)
                {
                    throw new DomainValidationException("Selected curriculum does not exist.");
                }

                var mappingExists = db.CurriculumSubjects.Any(x =>
                    x.CurriculumId == entity.CurriculumId.Value &&
                    x.GradeLevelId == section.GradeLevelId &&
                    x.SubjectId == entity.SubjectId);
                if (!mappingExists)
                {
                    throw new DomainValidationException("Selected subject is not mapped to the curriculum for this grade level.");
                }
            }

            if (entity.TeacherId.HasValue)
            {
                var teacher = db.Teachers.Find(entity.TeacherId.Value);
                if (teacher == null)
                {
                    throw new DomainValidationException("Selected teacher does not exist.");
                }

                if (teacher.Status != UserStatus.ACTIVE)
                {
                    throw new DomainValidationException("Selected teacher must be active.");
                }

                var teacherLoad = db.ClassOfferings.Count(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    x.SchoolYearId == entity.SchoolYearId &&
                    x.TeacherId == entity.TeacherId.Value &&
                    x.Status != ClassOfferingStatus.ARCHIVED);
                if (teacherLoad >= TeacherLoadLimit)
                {
                    throw new DomainValidationException($"Teacher load limit reached ({TeacherLoadLimit}).");
                }
            }

            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = System.DateTime.UtcNow;
            }
        }
    }
}

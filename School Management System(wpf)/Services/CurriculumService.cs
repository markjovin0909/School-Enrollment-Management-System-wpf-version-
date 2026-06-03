using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class CurriculumService : ICurriculumService
    {
        public IEnumerable<Curriculum> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new CurriculumRepository(db);
            return repo.GetAll();
        }

        public Curriculum? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new CurriculumRepository(db);
            return repo.GetById(id);
        }

        public void Create(Curriculum entity)
        {
            using var db = new AppDbContext();
            ValidateCurriculum(db, entity, null);

            var now = System.DateTime.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            entity.UpdatedAt = now;

            db.Curricula.Add(entity);
            db.SaveChanges();
        }

        public void Update(Curriculum entity)
        {
            using var db = new AppDbContext();
            var existing = db.Curricula.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("Curriculum record was not found.");
            }

            ValidateCurriculum(db, entity, existing.Id);

            existing.Name = entity.Name.Trim();
            existing.Description = string.IsNullOrWhiteSpace(entity.Description) ? null : entity.Description.Trim();
            existing.IsActive = entity.IsActive;
            existing.UpdatedAt = System.DateTime.UtcNow;
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new CurriculumRepository(db);
            repo.Delete(id);
        }

        private static void ValidateCurriculum(AppDbContext db, Curriculum entity, long? excludeId)
        {
            if (entity == null)
            {
                throw new DomainValidationException("Curriculum data is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                throw new DomainValidationException("Curriculum name is required.");
            }

            var normalizedName = entity.Name.Trim().ToLower();
            var duplicateName = db.Curricula
                .Any(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    x.Name != null && x.Name.ToLower() == normalizedName);
            if (duplicateName)
            {
                throw new DomainValidationException("Curriculum name already exists.");
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class TeacherService : ITeacherService
    {
        public IEnumerable<Teacher> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new TeacherRepository(db);
            return repo.GetAll();
        }

        public Teacher? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new TeacherRepository(db);
            return repo.GetById(id);
        }

        public void Create(Teacher entity)
        {
            using var db = new AppDbContext();
            ValidateTeacher(db, entity, null);

            var now = System.DateTime.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            entity.UpdatedAt = now;

            db.Teachers.Add(entity);
            db.SaveChanges();
        }

        public void Update(Teacher entity)
        {
            using var db = new AppDbContext();
            var existing = db.Teachers.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("Teacher record was not found.");
            }

            ValidateTeacher(db, entity, existing.Id);

            existing.UserId = entity.UserId;
            existing.ProfileImageUrl = string.IsNullOrWhiteSpace(entity.ProfileImageUrl) ? null : entity.ProfileImageUrl.Trim();
            existing.EmployeeNo = entity.EmployeeNo?.Trim();
            existing.FirstName = entity.FirstName.Trim();
            existing.LastName = entity.LastName.Trim();
            existing.MiddleName = string.IsNullOrWhiteSpace(entity.MiddleName) ? null : entity.MiddleName.Trim();
            existing.Email = string.IsNullOrWhiteSpace(entity.Email) ? null : entity.Email.Trim();
            existing.ContactNo = string.IsNullOrWhiteSpace(entity.ContactNo) ? null : entity.ContactNo.Trim();
            existing.Specialization = string.IsNullOrWhiteSpace(entity.Specialization) ? null : entity.Specialization.Trim();
            existing.AdvisoryAssignmentStatus = string.IsNullOrWhiteSpace(entity.AdvisoryAssignmentStatus) ? null : entity.AdvisoryAssignmentStatus.Trim();
            existing.EmploymentStatus = string.IsNullOrWhiteSpace(entity.EmploymentStatus) ? null : entity.EmploymentStatus.Trim();
            existing.HireDate = entity.HireDate?.Date;
            existing.Status = entity.Status;
            existing.UpdatedAt = System.DateTime.UtcNow;
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new TeacherRepository(db);
            repo.Delete(id);
        }

        private static void ValidateTeacher(AppDbContext db, Teacher entity, long? excludeId)
        {
            if (entity == null)
            {
                throw new DomainValidationException("Teacher data is required.");
            }

            if (entity.UserId <= 0 || !db.Users.Any(x => x.Id == entity.UserId))
            {
                throw new DomainValidationException("Teacher account link is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.EmployeeNo))
            {
                throw new DomainValidationException("Employee number is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.FirstName) || string.IsNullOrWhiteSpace(entity.LastName))
            {
                throw new DomainValidationException("Teacher first name and last name are required.");
            }

            var normalizedEmployeeNo = entity.EmployeeNo.Trim();
            var duplicateEmployeeNo = db.Teachers
                .AsEnumerable()
                .Any(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    string.Equals((x.EmployeeNo ?? string.Empty).Trim(), normalizedEmployeeNo, System.StringComparison.OrdinalIgnoreCase));
            if (duplicateEmployeeNo)
            {
                throw new DomainValidationException("Employee number already exists.");
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class StudentService : IStudentService
    {
        public IEnumerable<Student> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new StudentRepository(db);
            return repo.GetAll();
        }

        public Student? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new StudentRepository(db);
            return repo.GetById(id);
        }

        public void Create(Student entity)
        {
            using var db = new AppDbContext();
            ValidateStudent(db, entity, null);

            var now = System.DateTime.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            entity.UpdatedAt = now;

            db.Students.Add(entity);
            db.SaveChanges();
        }

        public void Update(Student entity)
        {
            using var db = new AppDbContext();
            var existing = db.Students.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("Student record was not found.");
            }

            ValidateStudent(db, entity, existing.Id);

            existing.UserId = entity.UserId;
            existing.ProfileImageUrl = string.IsNullOrWhiteSpace(entity.ProfileImageUrl) ? null : entity.ProfileImageUrl.Trim();
            existing.Lrn = entity.Lrn.Trim();
            existing.StudentNumber = entity.StudentNumber.Trim();
            existing.FirstName = entity.FirstName.Trim();
            existing.LastName = entity.LastName.Trim();
            existing.MiddleName = string.IsNullOrWhiteSpace(entity.MiddleName) ? null : entity.MiddleName.Trim();
            existing.Birthdate = entity.Birthdate?.Date;
            existing.Age = entity.Age;
            existing.Sex = entity.Sex;
            existing.Address = string.IsNullOrWhiteSpace(entity.Address) ? null : entity.Address.Trim();
            existing.ContactNo = string.IsNullOrWhiteSpace(entity.ContactNo) ? null : entity.ContactNo.Trim();
            existing.GuardianName = string.IsNullOrWhiteSpace(entity.GuardianName) ? null : entity.GuardianName.Trim();
            existing.GuardianContact = string.IsNullOrWhiteSpace(entity.GuardianContact) ? null : entity.GuardianContact.Trim();
            existing.PreviousSchool = string.IsNullOrWhiteSpace(entity.PreviousSchool) ? null : entity.PreviousSchool.Trim();
            existing.PreferredGradeLevelId = entity.PreferredGradeLevelId;
            existing.PreferredCurriculumId = entity.PreferredCurriculumId;
            existing.Status = entity.Status;
            existing.UpdatedAt = System.DateTime.UtcNow;
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new StudentRepository(db);
            repo.Delete(id);
        }

        private static void ValidateStudent(AppDbContext db, Student entity, long? excludeId)
        {
            if (entity == null)
            {
                throw new DomainValidationException("Student data is required.");
            }

            if (entity.UserId <= 0 || !db.Users.Any(x => x.Id == entity.UserId))
            {
                throw new DomainValidationException("Student account link is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.StudentNumber))
            {
                throw new DomainValidationException("Student number is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.Lrn))
            {
                throw new DomainValidationException("LRN is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.FirstName) || string.IsNullOrWhiteSpace(entity.LastName))
            {
                throw new DomainValidationException("Student first name and last name are required.");
            }

            var normalizedStudentNumber = entity.StudentNumber.Trim().ToLower();
            var duplicateStudentNumber = db.Students
                .Any(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    x.StudentNumber.ToLower() == normalizedStudentNumber);
            if (duplicateStudentNumber)
            {
                throw new DomainValidationException("Student number already exists.");
            }

            var normalizedLrn = entity.Lrn.Trim().ToLower();
            var duplicateLrn = db.Students
                .Any(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    x.Lrn.ToLower() == normalizedLrn);
            if (duplicateLrn)
            {
                throw new DomainValidationException("LRN already exists.");
            }

            var normalizedFirstName = entity.FirstName.Trim().ToLower();
            var normalizedLastName = entity.LastName.Trim().ToLower();
            var normalizedMiddleName = (entity.MiddleName ?? string.Empty).Trim().ToLower();
            if (entity.Birthdate.HasValue)
            {
                var birthdateValue = entity.Birthdate.Value.Date;
                var duplicateIdentity = db.Students
                    .Any(x =>
                        (!excludeId.HasValue || x.Id != excludeId.Value) &&
                        x.Birthdate != null && x.Birthdate.Value == birthdateValue &&
                        x.FirstName.ToLower() == normalizedFirstName &&
                        x.LastName.ToLower() == normalizedLastName &&
                        (x.MiddleName == null ? "" : x.MiddleName.ToLower()) == normalizedMiddleName);
                if (duplicateIdentity)
                {
                    throw new DomainValidationException("A student with the same full name and birthdate already exists.");
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class UserService : IUserService
    {
        public IEnumerable<User> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new UserRepository(db);
            return repo.GetAll();
        }

        public User? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new UserRepository(db);
            return repo.GetById(id);
        }

        public void Create(User entity)
        {
            using var db = new AppDbContext();
            ValidateUser(db, entity, null);

            var now = System.DateTime.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            entity.UpdatedAt = now;

            db.Users.Add(entity);
            db.SaveChanges();
        }

        public void Update(User entity)
        {
            using var db = new AppDbContext();
            var existing = db.Users.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("User account was not found.");
            }

            ValidateUser(db, entity, existing.Id);

            existing.Username = entity.Username.Trim();
            existing.PasswordHash = entity.PasswordHash;
            existing.Role = entity.Role;
            existing.CanLogin = entity.CanLogin;
            existing.Status = entity.Status;
            existing.UpdatedAt = System.DateTime.UtcNow;
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new UserRepository(db);
            repo.Delete(id);
        }

        private static void ValidateUser(AppDbContext db, User entity, long? excludeId)
        {
            if (entity == null)
            {
                throw new DomainValidationException("User account data is required.");
            }

            if (string.IsNullOrWhiteSpace(entity.Username))
            {
                throw new DomainValidationException("Account ID is required.");
            }

            var normalizedUsername = entity.Username.Trim();
            var duplicateUsername = db.Users
                .AsEnumerable()
                .Any(x =>
                    (!excludeId.HasValue || x.Id != excludeId.Value) &&
                    string.Equals((x.Username ?? string.Empty).Trim(), normalizedUsername, System.StringComparison.OrdinalIgnoreCase));
            if (duplicateUsername)
            {
                throw new DomainValidationException("Account ID already exists.");
            }

            if (string.IsNullOrWhiteSpace(entity.PasswordHash))
            {
                throw new DomainValidationException("Account password hash is required.");
            }
        }
    }
}

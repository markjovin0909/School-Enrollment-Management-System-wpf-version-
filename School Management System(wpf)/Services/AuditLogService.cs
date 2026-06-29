using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class AuditLogService
    {
        public IEnumerable<AuditLog> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new AuditLogRepository(db);
            return repo.GetAll();
        }

        public AuditLog? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new AuditLogRepository(db);
            return repo.GetById(id);
        }

        public void Create(AuditLog entity)
        {
            using var db = new AppDbContext();
            var repo = new AuditLogRepository(db);
            repo.Add(entity);
        }

        public void Update(AuditLog entity)
        {
            using var db = new AppDbContext();
            var repo = new AuditLogRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new AuditLogRepository(db);
            repo.Delete(id);
        }
    }
}

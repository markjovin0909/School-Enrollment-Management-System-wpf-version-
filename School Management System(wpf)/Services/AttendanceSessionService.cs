using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class AttendanceSessionService
    {
        public IEnumerable<AttendanceSession> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new AttendanceSessionRepository(db);
            return repo.GetAll();
        }

        public AttendanceSession? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new AttendanceSessionRepository(db);
            return repo.GetById(id);
        }

        public void Create(AttendanceSession entity)
        {
            using var db = new AppDbContext();
            var repo = new AttendanceSessionRepository(db);
            repo.Add(entity);
        }

        public void Update(AttendanceSession entity)
        {
            using var db = new AppDbContext();
            var repo = new AttendanceSessionRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new AttendanceSessionRepository(db);
            repo.Delete(id);
        }
    }
}

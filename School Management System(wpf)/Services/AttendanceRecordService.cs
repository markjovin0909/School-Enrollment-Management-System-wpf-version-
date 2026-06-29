using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class AttendanceRecordService
    {
        public IEnumerable<AttendanceRecord> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new AttendanceRecordRepository(db);
            return repo.GetAll();
        }

        public AttendanceRecord? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new AttendanceRecordRepository(db);
            return repo.GetById(id);
        }

        public void Create(AttendanceRecord entity)
        {
            using var db = new AppDbContext();
            var repo = new AttendanceRecordRepository(db);
            repo.Add(entity);
        }

        public void Update(AttendanceRecord entity)
        {
            using var db = new AppDbContext();
            var repo = new AttendanceRecordRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new AttendanceRecordRepository(db);
            repo.Delete(id);
        }
    }
}

using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class ClassScheduleService
    {
        public IEnumerable<ClassSchedule> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new ClassScheduleRepository(db);
            return repo.GetAll();
        }

        public ClassSchedule? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new ClassScheduleRepository(db);
            return repo.GetById(id);
        }

        public void Create(ClassSchedule entity)
        {
            using var db = new AppDbContext();
            var repo = new ClassScheduleRepository(db);
            repo.Add(entity);
        }

        public void Update(ClassSchedule entity)
        {
            using var db = new AppDbContext();
            var repo = new ClassScheduleRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new ClassScheduleRepository(db);
            repo.Delete(id);
        }
    }
}

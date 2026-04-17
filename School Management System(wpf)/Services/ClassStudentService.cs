using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class ClassStudentService : IClassStudentService
    {
        public IEnumerable<ClassStudent> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new ClassStudentRepository(db);
            return repo.GetAll();
        }

        public ClassStudent? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new ClassStudentRepository(db);
            return repo.GetById(id);
        }

        public void Create(ClassStudent entity)
        {
            using var db = new AppDbContext();
            var repo = new ClassStudentRepository(db);
            repo.Add(entity);
        }

        public void Update(ClassStudent entity)
        {
            using var db = new AppDbContext();
            var repo = new ClassStudentRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new ClassStudentRepository(db);
            repo.Delete(id);
        }
    }
}

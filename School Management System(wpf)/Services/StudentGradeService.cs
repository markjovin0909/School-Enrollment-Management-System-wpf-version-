using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class StudentGradeService
    {
        public IEnumerable<StudentGrade> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new StudentGradeRepository(db);
            return repo.GetAll();
        }

        public StudentGrade? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new StudentGradeRepository(db);
            return repo.GetById(id);
        }

        public void Create(StudentGrade entity)
        {
            using var db = new AppDbContext();
            var repo = new StudentGradeRepository(db);
            repo.Add(entity);
        }

        public void Update(StudentGrade entity)
        {
            using var db = new AppDbContext();
            var repo = new StudentGradeRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new StudentGradeRepository(db);
            repo.Delete(id);
        }
    }
}

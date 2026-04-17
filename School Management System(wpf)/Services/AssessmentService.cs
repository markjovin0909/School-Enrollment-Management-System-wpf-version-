using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class AssessmentService : IAssessmentService
    {
        public IEnumerable<Assessment> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new AssessmentRepository(db);
            return repo.GetAll();
        }

        public Assessment? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new AssessmentRepository(db);
            return repo.GetById(id);
        }

        public void Create(Assessment entity)
        {
            using var db = new AppDbContext();
            var repo = new AssessmentRepository(db);
            repo.Add(entity);
        }

        public void Update(Assessment entity)
        {
            using var db = new AppDbContext();
            var repo = new AssessmentRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new AssessmentRepository(db);
            repo.Delete(id);
        }
    }
}

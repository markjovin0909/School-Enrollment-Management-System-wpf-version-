using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class GradeComponentService
    {
        public IEnumerable<GradeComponent> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new GradeComponentRepository(db);
            return repo.GetAll();
        }

        public GradeComponent? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new GradeComponentRepository(db);
            return repo.GetById(id);
        }

        public void Create(GradeComponent entity)
        {
            using var db = new AppDbContext();
            var repo = new GradeComponentRepository(db);
            repo.Add(entity);
        }

        public void Update(GradeComponent entity)
        {
            using var db = new AppDbContext();
            var repo = new GradeComponentRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new GradeComponentRepository(db);
            repo.Delete(id);
        }
    }
}

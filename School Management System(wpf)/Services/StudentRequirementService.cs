using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class StudentRequirementService : IStudentRequirementService
    {
        public IEnumerable<StudentRequirement> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new StudentRequirementRepository(db);
            return repo.GetAll();
        }

        public StudentRequirement? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new StudentRequirementRepository(db);
            return repo.GetById(id);
        }

        public void Create(StudentRequirement entity)
        {
            using var db = new AppDbContext();
            var repo = new StudentRequirementRepository(db);
            repo.Add(entity);
        }

        public void Update(StudentRequirement entity)
        {
            using var db = new AppDbContext();
            var repo = new StudentRequirementRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new StudentRequirementRepository(db);
            repo.Delete(id);
        }
    }
}

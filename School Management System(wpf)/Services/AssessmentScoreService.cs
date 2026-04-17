using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class AssessmentScoreService : IAssessmentScoreService
    {
        public IEnumerable<AssessmentScore> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new AssessmentScoreRepository(db);
            return repo.GetAll();
        }

        public AssessmentScore? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new AssessmentScoreRepository(db);
            return repo.GetById(id);
        }

        public void Create(AssessmentScore entity)
        {
            using var db = new AppDbContext();
            var repo = new AssessmentScoreRepository(db);
            repo.Add(entity);
        }

        public void Update(AssessmentScore entity)
        {
            using var db = new AppDbContext();
            var repo = new AssessmentScoreRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new AssessmentScoreRepository(db);
            repo.Delete(id);
        }
    }
}

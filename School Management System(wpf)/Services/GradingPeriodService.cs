using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class GradingPeriodService : IGradingPeriodService
    {
        public IEnumerable<GradingPeriod> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new GradingPeriodRepository(db);
            return repo.GetAll();
        }

        public GradingPeriod? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new GradingPeriodRepository(db);
            return repo.GetById(id);
        }

        public void Create(GradingPeriod entity)
        {
            using var db = new AppDbContext();
            var repo = new GradingPeriodRepository(db);
            repo.Add(entity);
        }

        public void Update(GradingPeriod entity)
        {
            using var db = new AppDbContext();
            var repo = new GradingPeriodRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new GradingPeriodRepository(db);
            repo.Delete(id);
        }
    }
}

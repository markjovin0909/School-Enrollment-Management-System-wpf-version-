using System;
using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class GradingPeriodService
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

        /// <summary>
        /// Marks the given period OPEN for the school year and closes other OPEN periods.
        /// LOCKED/POSTED periods are left unchanged.
        /// </summary>
        public void SetCurrentOpenPeriod(long schoolYearId, long gradingPeriodId)
        {
            using var db = new AppDbContext();
            var periods = db.Set<GradingPeriod>()
                .Where(x => x.SchoolYearId == schoolYearId)
                .ToList();

            var target = periods.FirstOrDefault(x => x.Id == gradingPeriodId);
            if (target == null)
            {
                throw new DomainValidationException("Selected grading period was not found for the school year.");
            }

            if (target.Status is GradingPeriodStatus.LOCKED or GradingPeriodStatus.POSTED)
            {
                throw new DomainValidationException($"Cannot open grading period '{target.Name}' because its status is {target.Status}.");
            }

            var now = DateTime.UtcNow;
            foreach (var period in periods)
            {
                if (period.Id == gradingPeriodId)
                {
                    period.Status = GradingPeriodStatus.OPEN;
                    period.UpdatedAt = now;
                    continue;
                }

                if (period.Status == GradingPeriodStatus.OPEN)
                {
                    period.Status = GradingPeriodStatus.CLOSED;
                    period.UpdatedAt = now;
                }
            }

            db.SaveChanges();
        }
    }
}

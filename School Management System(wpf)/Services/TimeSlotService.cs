using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class TimeSlotService
    {
        public IEnumerable<TimeSlot> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new TimeSlotRepository(db);
            return repo.GetAll();
        }

        public TimeSlot? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new TimeSlotRepository(db);
            return repo.GetById(id);
        }

        public void Create(TimeSlot entity)
        {
            using var db = new AppDbContext();
            var repo = new TimeSlotRepository(db);
            repo.Add(entity);
        }

        public void Update(TimeSlot entity)
        {
            using var db = new AppDbContext();
            var repo = new TimeSlotRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new TimeSlotRepository(db);
            repo.Delete(id);
        }
    }
}

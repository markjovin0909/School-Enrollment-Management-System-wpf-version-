using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class AnnouncementService
    {
        public IEnumerable<Announcement> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new AnnouncementRepository(db);
            return repo.GetAll();
        }

        public Announcement? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new AnnouncementRepository(db);
            return repo.GetById(id);
        }

        public void Create(Announcement entity)
        {
            using var db = new AppDbContext();
            var repo = new AnnouncementRepository(db);
            repo.Add(entity);
        }

        public void Update(Announcement entity)
        {
            using var db = new AppDbContext();
            var repo = new AnnouncementRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new AnnouncementRepository(db);
            repo.Delete(id);
        }
    }
}

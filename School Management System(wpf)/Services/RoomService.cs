using System.Collections.Generic;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class RoomService : IRoomService
    {
        public IEnumerable<Room> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new RoomRepository(db);
            return repo.GetAll();
        }

        public Room? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new RoomRepository(db);
            return repo.GetById(id);
        }

        public void Create(Room entity)
        {
            using var db = new AppDbContext();
            var repo = new RoomRepository(db);
            repo.Add(entity);
        }

        public void Update(Room entity)
        {
            using var db = new AppDbContext();
            var repo = new RoomRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new RoomRepository(db);
            repo.Delete(id);
        }
    }
}

using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IRoomService
    {
        IEnumerable<Room> GetAll();
        Room? GetById(long id);
        void Create(Room entity);
        void Update(Room entity);
        void Delete(long id);
    }
}

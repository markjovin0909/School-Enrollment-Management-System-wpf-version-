using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IAnnouncementService
    {
        IEnumerable<Announcement> GetAll();
        Announcement? GetById(long id);
        void Create(Announcement entity);
        void Update(Announcement entity);
        void Delete(long id);
    }
}

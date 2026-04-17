using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class AnnouncementRepository : BaseRepository<Models.Announcement>, IAnnouncementRepository
    {
        public AnnouncementRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

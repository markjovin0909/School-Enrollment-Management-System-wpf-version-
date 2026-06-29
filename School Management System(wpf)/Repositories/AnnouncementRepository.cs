
namespace School_Management_System.Repositories
{
    internal class AnnouncementRepository : BaseRepository<Models.Announcement>
    {
        public AnnouncementRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class RoomRepository : BaseRepository<Models.Room>, IRoomRepository
    {
        public RoomRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

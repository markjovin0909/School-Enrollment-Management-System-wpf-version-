
namespace School_Management_System.Repositories
{
    internal class RoomRepository : BaseRepository<Models.Room>
    {
        public RoomRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

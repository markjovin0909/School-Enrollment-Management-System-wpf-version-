
namespace School_Management_System.Repositories
{
    internal class AttendanceSessionRepository : BaseRepository<Models.AttendanceSession>
    {
        public AttendanceSessionRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

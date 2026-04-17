using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class AttendanceSessionRepository : BaseRepository<Models.AttendanceSession>, IAttendanceSessionRepository
    {
        public AttendanceSessionRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

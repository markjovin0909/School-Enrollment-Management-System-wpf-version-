using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class AttendanceRecordRepository : BaseRepository<Models.AttendanceRecord>, IAttendanceRecordRepository
    {
        public AttendanceRecordRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

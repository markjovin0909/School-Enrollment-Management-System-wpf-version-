
namespace School_Management_System.Repositories
{
    internal class AttendanceRecordRepository : BaseRepository<Models.AttendanceRecord>
    {
        public AttendanceRecordRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class ClassScheduleRepository : BaseRepository<Models.ClassSchedule>, IClassScheduleRepository
    {
        public ClassScheduleRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

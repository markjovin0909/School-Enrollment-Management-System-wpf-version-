
namespace School_Management_System.Repositories
{
    internal class ClassScheduleRepository : BaseRepository<Models.ClassSchedule>
    {
        public ClassScheduleRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

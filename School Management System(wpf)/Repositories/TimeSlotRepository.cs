
namespace School_Management_System.Repositories
{
    internal class TimeSlotRepository : BaseRepository<Models.TimeSlot>
    {
        public TimeSlotRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class TimeSlotRepository : BaseRepository<Models.TimeSlot>, ITimeSlotRepository
    {
        public TimeSlotRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class GradingPeriodRepository : BaseRepository<Models.GradingPeriod>, IGradingPeriodRepository
    {
        public GradingPeriodRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

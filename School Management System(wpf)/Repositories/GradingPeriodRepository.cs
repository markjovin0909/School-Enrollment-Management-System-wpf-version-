
namespace School_Management_System.Repositories
{
    internal class GradingPeriodRepository : BaseRepository<Models.GradingPeriod>
    {
        public GradingPeriodRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class SectionRepository : BaseRepository<Models.Section>, ISectionRepository
    {
        public SectionRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class SchoolYearRepository : BaseRepository<Models.SchoolYear>, ISchoolYearRepository
    {
        public SchoolYearRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

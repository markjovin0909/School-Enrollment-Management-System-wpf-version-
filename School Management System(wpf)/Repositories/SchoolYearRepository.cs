
namespace School_Management_System.Repositories
{
    internal class SchoolYearRepository : BaseRepository<Models.SchoolYear>
    {
        public SchoolYearRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

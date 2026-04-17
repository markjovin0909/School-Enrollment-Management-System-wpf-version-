using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class GradeComponentRepository : BaseRepository<Models.GradeComponent>, IGradeComponentRepository
    {
        public GradeComponentRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

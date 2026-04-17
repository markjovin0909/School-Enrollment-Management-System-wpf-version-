using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class GradeLevelRepository : BaseRepository<Models.GradeLevel>, IGradeLevelRepository
    {
        public GradeLevelRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

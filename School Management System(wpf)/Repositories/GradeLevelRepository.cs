
namespace School_Management_System.Repositories
{
    internal class GradeLevelRepository : BaseRepository<Models.GradeLevel>
    {
        public GradeLevelRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

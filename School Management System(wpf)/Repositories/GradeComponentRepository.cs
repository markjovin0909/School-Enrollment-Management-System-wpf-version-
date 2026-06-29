
namespace School_Management_System.Repositories
{
    internal class GradeComponentRepository : BaseRepository<Models.GradeComponent>
    {
        public GradeComponentRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

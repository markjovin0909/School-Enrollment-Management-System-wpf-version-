
namespace School_Management_System.Repositories
{
    internal class CurriculumRepository : BaseRepository<Models.Curriculum>
    {
        public CurriculumRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

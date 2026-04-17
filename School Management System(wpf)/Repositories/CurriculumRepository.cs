using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class CurriculumRepository : BaseRepository<Models.Curriculum>, ICurriculumRepository
    {
        public CurriculumRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

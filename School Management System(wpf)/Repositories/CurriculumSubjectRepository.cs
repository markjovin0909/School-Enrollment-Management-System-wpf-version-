using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class CurriculumSubjectRepository : BaseRepository<Models.CurriculumSubject>, ICurriculumSubjectRepository
    {
        public CurriculumSubjectRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

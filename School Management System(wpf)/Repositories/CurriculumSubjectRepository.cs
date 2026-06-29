
namespace School_Management_System.Repositories
{
    internal class CurriculumSubjectRepository : BaseRepository<Models.CurriculumSubject>
    {
        public CurriculumSubjectRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

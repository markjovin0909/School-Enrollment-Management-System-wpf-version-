using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class AssessmentRepository : BaseRepository<Models.Assessment>, IAssessmentRepository
    {
        public AssessmentRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

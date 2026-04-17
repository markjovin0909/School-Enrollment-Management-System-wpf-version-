using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class AssessmentScoreRepository : BaseRepository<Models.AssessmentScore>, IAssessmentScoreRepository
    {
        public AssessmentScoreRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

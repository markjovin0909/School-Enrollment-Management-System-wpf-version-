
namespace School_Management_System.Repositories
{
    internal class AssessmentScoreRepository : BaseRepository<Models.AssessmentScore>
    {
        public AssessmentScoreRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

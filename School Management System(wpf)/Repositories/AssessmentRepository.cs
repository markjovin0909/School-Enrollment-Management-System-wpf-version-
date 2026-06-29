
namespace School_Management_System.Repositories
{
    internal class AssessmentRepository : BaseRepository<Models.Assessment>
    {
        public AssessmentRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

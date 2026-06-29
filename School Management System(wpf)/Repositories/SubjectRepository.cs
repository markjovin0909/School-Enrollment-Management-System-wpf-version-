
namespace School_Management_System.Repositories
{
    internal class SubjectRepository : BaseRepository<Models.Subject>
    {
        public SubjectRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

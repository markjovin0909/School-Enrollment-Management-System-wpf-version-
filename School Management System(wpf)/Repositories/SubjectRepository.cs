using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class SubjectRepository : BaseRepository<Models.Subject>, ISubjectRepository
    {
        public SubjectRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

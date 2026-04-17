using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class ClassStudentRepository : BaseRepository<Models.ClassStudent>, IClassStudentRepository
    {
        public ClassStudentRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

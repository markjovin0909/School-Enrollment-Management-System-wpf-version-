using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class StudentRepository : BaseRepository<Models.Student>, IStudentRepository
    {
        public StudentRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

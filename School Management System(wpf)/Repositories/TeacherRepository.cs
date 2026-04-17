using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class TeacherRepository : BaseRepository<Models.Teacher>, ITeacherRepository
    {
        public TeacherRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}


namespace School_Management_System.Repositories
{
    internal class TeacherRepository : BaseRepository<Models.Teacher>
    {
        public TeacherRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

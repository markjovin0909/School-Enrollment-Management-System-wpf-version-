
namespace School_Management_System.Repositories
{
    internal class ClassStudentRepository : BaseRepository<Models.ClassStudent>
    {
        public ClassStudentRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

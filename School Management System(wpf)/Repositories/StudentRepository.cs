
namespace School_Management_System.Repositories
{
    internal class StudentRepository : BaseRepository<Models.Student>
    {
        public StudentRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

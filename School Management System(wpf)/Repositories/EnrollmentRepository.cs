
namespace School_Management_System.Repositories
{
    internal class EnrollmentRepository : BaseRepository<Models.Enrollment>
    {
        public EnrollmentRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class EnrollmentRepository : BaseRepository<Models.Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class StudentRequirementRepository : BaseRepository<Models.StudentRequirement>, IStudentRequirementRepository
    {
        public StudentRequirementRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}


namespace School_Management_System.Repositories
{
    internal class StudentRequirementRepository : BaseRepository<Models.StudentRequirement>
    {
        public StudentRequirementRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

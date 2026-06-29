
namespace School_Management_System.Repositories
{
    internal class StudentGradeRepository : BaseRepository<Models.StudentGrade>
    {
        public StudentGradeRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

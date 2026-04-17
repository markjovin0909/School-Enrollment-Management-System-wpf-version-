using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class StudentGradeRepository : BaseRepository<Models.StudentGrade>, IStudentGradeRepository
    {
        public StudentGradeRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

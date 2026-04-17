using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IStudentGradeService
    {
        IEnumerable<StudentGrade> GetAll();
        StudentGrade? GetById(long id);
        void Create(StudentGrade entity);
        void Update(StudentGrade entity);
        void Delete(long id);
    }
}

using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IStudentService
    {
        IEnumerable<Student> GetAll();
        Student? GetById(long id);
        void Create(Student entity);
        void Update(Student entity);
        void Delete(long id);
    }
}

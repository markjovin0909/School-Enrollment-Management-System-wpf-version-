using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface ITeacherService
    {
        IEnumerable<Teacher> GetAll();
        Teacher? GetById(long id);
        void Create(Teacher entity);
        void Update(Teacher entity);
        void Delete(long id);
    }
}

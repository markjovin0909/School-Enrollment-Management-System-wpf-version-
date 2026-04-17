using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IClassStudentService
    {
        IEnumerable<ClassStudent> GetAll();
        ClassStudent? GetById(long id);
        void Create(ClassStudent entity);
        void Update(ClassStudent entity);
        void Delete(long id);
    }
}

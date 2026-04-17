using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface ISubjectService
    {
        IEnumerable<Subject> GetAll();
        Subject? GetById(long id);
        void Create(Subject entity);
        void Update(Subject entity);
        void Delete(long id);
    }
}

using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IGradeLevelService
    {
        IEnumerable<GradeLevel> GetAll();
        GradeLevel? GetById(long id);
        void Create(GradeLevel entity);
        void Update(GradeLevel entity);
        void Delete(long id);
    }
}

using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IGradeComponentService
    {
        IEnumerable<GradeComponent> GetAll();
        GradeComponent? GetById(long id);
        void Create(GradeComponent entity);
        void Update(GradeComponent entity);
        void Delete(long id);
    }
}

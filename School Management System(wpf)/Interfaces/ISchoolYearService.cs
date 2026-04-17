using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface ISchoolYearService
    {
        IEnumerable<SchoolYear> GetAll();
        SchoolYear? GetById(long id);
        void Create(SchoolYear entity);
        void Update(SchoolYear entity);
        void Delete(long id);
    }
}

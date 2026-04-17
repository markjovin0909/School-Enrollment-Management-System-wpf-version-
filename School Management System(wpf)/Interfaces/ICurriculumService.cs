using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface ICurriculumService
    {
        IEnumerable<Curriculum> GetAll();
        Curriculum? GetById(long id);
        void Create(Curriculum entity);
        void Update(Curriculum entity);
        void Delete(long id);
    }
}

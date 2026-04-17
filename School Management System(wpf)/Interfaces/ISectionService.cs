using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface ISectionService
    {
        IEnumerable<Section> GetAll();
        Section? GetById(long id);
        void Create(Section entity);
        void Update(Section entity);
        void Delete(long id);
    }
}

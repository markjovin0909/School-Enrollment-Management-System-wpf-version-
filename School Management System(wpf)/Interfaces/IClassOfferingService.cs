using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IClassOfferingService
    {
        IEnumerable<ClassOffering> GetAll();
        ClassOffering? GetById(long id);
        void Create(ClassOffering entity);
        void Update(ClassOffering entity);
        void Delete(long id);
    }
}

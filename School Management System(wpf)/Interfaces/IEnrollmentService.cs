using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IEnrollmentService
    {
        IEnumerable<Enrollment> GetAll();
        Enrollment? GetById(long id);
        void Create(Enrollment entity);
        void Update(Enrollment entity);
        void Delete(long id);
    }
}

using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IAssessmentService
    {
        IEnumerable<Assessment> GetAll();
        Assessment? GetById(long id);
        void Create(Assessment entity);
        void Update(Assessment entity);
        void Delete(long id);
    }
}

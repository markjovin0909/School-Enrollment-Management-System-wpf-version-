using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface ICurriculumSubjectService
    {
        IEnumerable<CurriculumSubject> GetAll();
        CurriculumSubject? GetById(long id);
        void Create(CurriculumSubject entity);
        void Update(CurriculumSubject entity);
        void Delete(long id);
    }
}

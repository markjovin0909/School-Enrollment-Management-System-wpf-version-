using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IAssessmentScoreService
    {
        IEnumerable<AssessmentScore> GetAll();
        AssessmentScore? GetById(long id);
        void Create(AssessmentScore entity);
        void Update(AssessmentScore entity);
        void Delete(long id);
    }
}

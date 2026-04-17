using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IGradingPeriodService
    {
        IEnumerable<GradingPeriod> GetAll();
        GradingPeriod? GetById(long id);
        void Create(GradingPeriod entity);
        void Update(GradingPeriod entity);
        void Delete(long id);
    }
}

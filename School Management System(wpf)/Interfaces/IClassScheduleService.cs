using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IClassScheduleService
    {
        IEnumerable<ClassSchedule> GetAll();
        ClassSchedule? GetById(long id);
        void Create(ClassSchedule entity);
        void Update(ClassSchedule entity);
        void Delete(long id);
    }
}

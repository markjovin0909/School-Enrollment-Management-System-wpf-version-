using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface ITimeSlotService
    {
        IEnumerable<TimeSlot> GetAll();
        TimeSlot? GetById(long id);
        void Create(TimeSlot entity);
        void Update(TimeSlot entity);
        void Delete(long id);
    }
}

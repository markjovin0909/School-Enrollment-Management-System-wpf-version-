using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IAttendanceSessionService
    {
        IEnumerable<AttendanceSession> GetAll();
        AttendanceSession? GetById(long id);
        void Create(AttendanceSession entity);
        void Update(AttendanceSession entity);
        void Delete(long id);
    }
}

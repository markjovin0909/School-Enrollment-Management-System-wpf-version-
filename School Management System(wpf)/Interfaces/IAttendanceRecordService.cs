using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IAttendanceRecordService
    {
        IEnumerable<AttendanceRecord> GetAll();
        AttendanceRecord? GetById(long id);
        void Create(AttendanceRecord entity);
        void Update(AttendanceRecord entity);
        void Delete(long id);
    }
}

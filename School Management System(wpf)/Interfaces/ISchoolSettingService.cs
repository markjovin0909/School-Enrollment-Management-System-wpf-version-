using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface ISchoolSettingService
    {
        IEnumerable<SchoolSetting> GetAll();
        SchoolSetting? GetById(long id);
        void Create(SchoolSetting entity);
        void Update(SchoolSetting entity);
        void Delete(long id);
    }
}

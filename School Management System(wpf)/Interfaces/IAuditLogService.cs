using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IAuditLogService
    {
        IEnumerable<AuditLog> GetAll();
        AuditLog? GetById(long id);
        void Create(AuditLog entity);
        void Update(AuditLog entity);
        void Delete(long id);
    }
}

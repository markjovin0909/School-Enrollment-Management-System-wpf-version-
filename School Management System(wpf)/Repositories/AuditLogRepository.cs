using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class AuditLogRepository : BaseRepository<Models.AuditLog>, IAuditLogRepository
    {
        public AuditLogRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}


namespace School_Management_System.Repositories
{
    internal class AuditLogRepository : BaseRepository<Models.AuditLog>
    {
        public AuditLogRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

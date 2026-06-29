
namespace School_Management_System.Repositories
{
    internal class ArchiveRecordRepository : BaseRepository<Models.ArchiveRecord>
    {
        public ArchiveRecordRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

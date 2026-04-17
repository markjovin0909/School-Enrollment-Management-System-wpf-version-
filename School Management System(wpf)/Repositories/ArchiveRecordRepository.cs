using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class ArchiveRecordRepository : BaseRepository<Models.ArchiveRecord>, IArchiveRecordRepository
    {
        public ArchiveRecordRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

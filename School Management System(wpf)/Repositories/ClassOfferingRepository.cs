using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class ClassOfferingRepository : BaseRepository<Models.ClassOffering>, IClassOfferingRepository
    {
        public ClassOfferingRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

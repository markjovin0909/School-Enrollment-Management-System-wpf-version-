
namespace School_Management_System.Repositories
{
    internal class ClassOfferingRepository : BaseRepository<Models.ClassOffering>
    {
        public ClassOfferingRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

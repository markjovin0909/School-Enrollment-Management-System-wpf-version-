
namespace School_Management_System.Repositories
{
    internal class SectionRepository : BaseRepository<Models.Section>
    {
        public SectionRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

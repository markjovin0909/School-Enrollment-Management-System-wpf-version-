
namespace School_Management_System.Repositories
{
    internal class SchoolSettingRepository : BaseRepository<Models.SchoolSetting>
    {
        public SchoolSettingRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

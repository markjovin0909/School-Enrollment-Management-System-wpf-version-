using School_Management_System.Interfaces;

namespace School_Management_System.Repositories
{
    internal class SchoolSettingRepository : BaseRepository<Models.SchoolSetting>, ISchoolSettingRepository
    {
        public SchoolSettingRepository(Data.AppDbContext context) : base(context)
        {
        }
    }
}

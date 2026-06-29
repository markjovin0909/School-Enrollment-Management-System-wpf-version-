using System.Linq;

namespace School_Management_System.Repositories
{
    internal class UserRepository : BaseRepository<Models.User>
    {
        public UserRepository(Data.AppDbContext context) : base(context)
        {
        }

        public Models.User? GetByUsername(string username)
        {
            var normalized = username.Trim().ToLower();
            return _context.Users.FirstOrDefault(u => u.Username.ToLower() == normalized);
        }

        public bool ExistsUsername(string username, long? excludeId = null)
        {
            var normalized = username.Trim().ToLower();
            return _context.Users.Any(u => u.Username.ToLower() == normalized && (!excludeId.HasValue || u.Id != excludeId.Value));
        }
    }
}

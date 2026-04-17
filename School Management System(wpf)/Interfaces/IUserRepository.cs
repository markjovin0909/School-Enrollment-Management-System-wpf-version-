using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IUserRepository : IBaseRepository<User>
    {
        User? GetByUsername(string username);
        bool ExistsUsername(string username, long? excludeId = null);
    }
}

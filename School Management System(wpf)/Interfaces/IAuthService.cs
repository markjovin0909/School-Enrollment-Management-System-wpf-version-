using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Interfaces
{
    internal interface IAuthService
    {
        OperationResult<User> Authenticate(string username, string password);
        OperationResult<User> Register(User user, string password);
    }
}

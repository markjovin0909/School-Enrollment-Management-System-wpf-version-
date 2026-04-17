using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IUserService
    {
        IEnumerable<User> GetAll();
        User? GetById(long id);
        void Create(User entity);
        void Update(User entity);
        void Delete(long id);
    }
}

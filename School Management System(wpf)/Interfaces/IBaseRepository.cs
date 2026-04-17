using System.Collections.Generic;

namespace School_Management_System.Interfaces
{
    internal interface IBaseRepository<T>
        where T : class, IBaseModel
    {
        IEnumerable<T> GetAll();
        T? GetById(long id);
        void Add(T entity);
        void Update(T entity);
        void Delete(long id);
    }
}

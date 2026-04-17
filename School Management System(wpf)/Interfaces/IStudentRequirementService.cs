using System.Collections.Generic;
using School_Management_System.Models;

namespace School_Management_System.Interfaces
{
    internal interface IStudentRequirementService
    {
        IEnumerable<StudentRequirement> GetAll();
        StudentRequirement? GetById(long id);
        void Create(StudentRequirement entity);
        void Update(StudentRequirement entity);
        void Delete(long id);
    }
}

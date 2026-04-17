using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("curricula")]
    public class Curriculum : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("description")]
        public string? Description { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; } = true;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public List<CurriculumSubject> CurriculumSubjects { get; set; } = new();
        public List<Enrollment> Enrollments { get; set; } = new();
        public List<ClassOffering> ClassOfferings { get; set; } = new();
    }
}

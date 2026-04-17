using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("grade_levels")]
    public class GradeLevel : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("code")]
        public string Code { get; set; } = string.Empty;
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public List<Section> Sections { get; set; } = new();
        public List<Subject> Subjects { get; set; } = new();
        public List<CurriculumSubject> CurriculumSubjects { get; set; } = new();
        public List<Enrollment> Enrollments { get; set; } = new();
    }
}

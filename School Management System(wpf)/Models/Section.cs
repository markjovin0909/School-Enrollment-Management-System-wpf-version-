using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("sections")]
    public class Section : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("school_year_id")]
        public long SchoolYearId { get; set; }
        [Column("grade_level_id")]
        public long GradeLevelId { get; set; }
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("capacity")]
        public int? Capacity { get; set; }
        [Column("adviser_teacher_id")]
        public long? AdviserTeacherId { get; set; }
        [Column("is_archived")]
        public bool IsArchived { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public SchoolYear? SchoolYear { get; set; }
        public GradeLevel? GradeLevel { get; set; }
        public Teacher? AdviserTeacher { get; set; }
        public List<ClassOffering> ClassOfferings { get; set; } = new();
        public List<Enrollment> Enrollments { get; set; } = new();
    }
}

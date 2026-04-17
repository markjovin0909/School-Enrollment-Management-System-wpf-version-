using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("subjects")]
    public class Subject : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("code")]
        public string Code { get; set; } = string.Empty;
        [Column("title")]
        public string Title { get; set; } = string.Empty;
        [Column("description")]
        public string? Description { get; set; }
        [Column("grade_level_id")]
        public long? GradeLevelId { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; } = true;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public GradeLevel? GradeLevel { get; set; }
        public List<CurriculumSubject> CurriculumSubjects { get; set; } = new();
        public List<ClassOffering> ClassOfferings { get; set; } = new();
    }
}

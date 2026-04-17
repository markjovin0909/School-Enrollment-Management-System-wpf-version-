using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("curriculum_subjects")]
    public class CurriculumSubject : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("curriculum_id")]
        public long CurriculumId { get; set; }
        [Column("grade_level_id")]
        public long GradeLevelId { get; set; }
        [Column("subject_id")]
        public long SubjectId { get; set; }
        [Column("semester")]
        public byte? Semester { get; set; }
        [Column("is_required")]
        public bool IsRequired { get; set; } = true;
        [Column("sort_order")]
        public int SortOrder { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public Curriculum? Curriculum { get; set; }
        public GradeLevel? GradeLevel { get; set; }
        public Subject? Subject { get; set; }
    }
}

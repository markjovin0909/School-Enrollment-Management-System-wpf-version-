using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("class_offerings")]
    public class ClassOffering : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("school_year_id")]
        public long SchoolYearId { get; set; }
        [Column("section_id")]
        public long SectionId { get; set; }
        [Column("subject_id")]
        public long SubjectId { get; set; }
        [Column("teacher_id")]
        public long? TeacherId { get; set; }
        [Column("curriculum_id")]
        public long? CurriculumId { get; set; }
        [Column("status")]
        public ClassOfferingStatus Status { get; set; } = ClassOfferingStatus.DRAFT;
        [Column("room")]
        public string? Room { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public SchoolYear? SchoolYear { get; set; }
        public Section? Section { get; set; }
        public Subject? Subject { get; set; }
        public Teacher? Teacher { get; set; }
        public Curriculum? Curriculum { get; set; }
        public List<ClassSchedule> ClassSchedules { get; set; } = new();
        public List<ClassStudent> ClassStudents { get; set; } = new();
    }
}

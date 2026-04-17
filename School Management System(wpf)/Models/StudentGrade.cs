using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("student_grades")]
    public class StudentGrade : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("class_offering_id")]
        public long ClassOfferingId { get; set; }
        [Column("grading_period_id")]
        public long GradingPeriodId { get; set; }
        [Column("student_id")]
        public long StudentId { get; set; }
        [Column("written_works", TypeName = "decimal(5,2)")]
        public decimal? WrittenWorks { get; set; }
        [Column("performance_tasks", TypeName = "decimal(5,2)")]
        public decimal? PerformanceTasks { get; set; }
        [Column("quarterly_assessment", TypeName = "decimal(5,2)")]
        public decimal? QuarterlyAssessment { get; set; }
        [Column("quarter_grade", TypeName = "decimal(5,2)")]
        public decimal? QuarterGrade { get; set; }
        [Column("submitted_at")]
        public DateTime? SubmittedAt { get; set; }
        [Column("locked_at")]
        public DateTime? LockedAt { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ClassOffering? ClassOffering { get; set; }
        public GradingPeriod? GradingPeriod { get; set; }
        public Student? Student { get; set; }
    }
}

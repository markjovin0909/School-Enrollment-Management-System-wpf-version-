using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("assessment_scores")]
    public class AssessmentScore : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("assessment_id")]
        public long AssessmentId { get; set; }
        [Column("student_id")]
        public long StudentId { get; set; }
        [Column("score", TypeName = "decimal(8,2)")]
        public decimal Score { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public Assessment? Assessment { get; set; }
        public Student? Student { get; set; }
    }
}

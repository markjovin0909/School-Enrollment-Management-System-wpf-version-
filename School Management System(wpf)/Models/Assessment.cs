using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("assessments")]
    public class Assessment : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("class_offering_id")]
        public long ClassOfferingId { get; set; }
        [Column("grading_period_id")]
        public long GradingPeriodId { get; set; }
        [Column("component_id")]
        public long ComponentId { get; set; }
        [Column("title")]
        public string Title { get; set; } = string.Empty;
        [Column("max_score", TypeName = "decimal(8,2)")]
        public decimal MaxScore { get; set; }
        [Column("date_given")]
        public DateTime? DateGiven { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ClassOffering? ClassOffering { get; set; }
        public GradingPeriod? GradingPeriod { get; set; }
        public GradeComponent? Component { get; set; }
        public List<AssessmentScore> AssessmentScores { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("grading_periods")]
    public class GradingPeriod : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("school_year_id")]
        public long SchoolYearId { get; set; }
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("start_date")]
        public DateTime? StartDate { get; set; }
        [Column("end_date")]
        public DateTime? EndDate { get; set; }
        [Column("status")]
        public GradingPeriodStatus Status { get; set; } = GradingPeriodStatus.CLOSED;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public SchoolYear? SchoolYear { get; set; }
        public List<StudentGrade> StudentGrades { get; set; } = new();
        public List<Assessment> Assessments { get; set; } = new();
    }
}

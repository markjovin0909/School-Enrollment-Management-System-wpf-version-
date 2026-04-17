using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("school_years")]
    public class SchoolYear : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("start_date")]
        public DateTime? StartDate { get; set; }
        [Column("end_date")]
        public DateTime? EndDate { get; set; }
        [Column("enrollment_open_date")]
        public DateTime? EnrollmentOpenDate { get; set; }
        [Column("enrollment_close_date")]
        public DateTime? EnrollmentCloseDate { get; set; }
        [Column("status")]
        public SchoolYearStatus Status { get; set; } = SchoolYearStatus.PLANNING;
        [Column("is_archived")]
        public bool IsArchived { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public List<Section> Sections { get; set; } = new();
        public List<Enrollment> Enrollments { get; set; } = new();
        public List<ClassOffering> ClassOfferings { get; set; } = new();
    }
}

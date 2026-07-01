using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("school_settings")]
    public class SchoolSetting : Interfaces.IBaseModel
    {
        public long Id { get; set; }

        [Column("school_name")]
        public string SchoolName { get; set; } = string.Empty;

        [Column("school_code")]
        public string SchoolCode { get; set; } = string.Empty;

        [Column("school_address")]
        public string SchoolAddress { get; set; } = string.Empty;

        [Column("principal_name")]
        public string PrincipalName { get; set; } = string.Empty;

        [Column("grading_setup")]
        public string GradingSetup { get; set; } = string.Empty;

        [Column("enrollment_configuration")]
        public string EnrollmentConfiguration { get; set; } = string.Empty;

        [Column("enrollment_open_date")]
        public DateTime? EnrollmentOpenDate { get; set; }

        [Column("enrollment_close_date")]
        public DateTime? EnrollmentCloseDate { get; set; }

        [Column("print_header_line1")]
        public string? PrintHeaderLine1 { get; set; }

        [Column("print_header_line2")]
        public string? PrintHeaderLine2 { get; set; }

        [Column("school_logo_file_key")]
        public string? SchoolLogoPath { get; set; }

        [Column("student_number_prefix")]
        public string StudentNumberPrefix { get; set; } = "S";

        [Column("next_student_number")]
        public int NextStudentNumber { get; set; } = 1;

        [Column("default_section_capacity")]
        public int DefaultSectionCapacity { get; set; } = 45;

        [Column("default_grade_level_ids")]
        public string? DefaultGradeLevelIds { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

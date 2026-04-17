using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("enrollments")]
    public class Enrollment : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("school_year_id")]
        public long SchoolYearId { get; set; }
        [Column("student_id")]
        public long StudentId { get; set; }
        [Column("grade_level_id")]
        public long GradeLevelId { get; set; }
        [Column("section_id")]
        public long SectionId { get; set; }
        [Column("curriculum_id")]
        public long CurriculumId { get; set; }
        [Column("status")]
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.ENROLLED;
        [Column("approval_status")]
        public EnrollmentApprovalStatus ApprovalStatus { get; set; } = EnrollmentApprovalStatus.PENDING;
        [Column("enrollment_type")]
        public string EnrollmentType { get; set; } = "NEW";
        [Column("waitlist_position")]
        public int? WaitlistPosition { get; set; }
        [Column("approved_by_user_id")]
        public long? ApprovedByUserId { get; set; }
        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }
        [Column("notes")]
        public string? Notes { get; set; }
        [Column("enrolled_at")]
        public DateTime EnrolledAt { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public SchoolYear? SchoolYear { get; set; }
        public Student? Student { get; set; }
        public GradeLevel? GradeLevel { get; set; }
        public Section? Section { get; set; }
        public Curriculum? Curriculum { get; set; }
    }
}

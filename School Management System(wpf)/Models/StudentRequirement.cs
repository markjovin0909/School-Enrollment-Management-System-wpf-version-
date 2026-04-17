using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("student_requirements")]
    public class StudentRequirement : Interfaces.IBaseModel
    {
        public long Id { get; set; }

        [Column("student_id")]
        public long StudentId { get; set; }

        [Column("requirement_name")]
        public string RequirementName { get; set; } = string.Empty;

        [Column("is_submitted")]
        public bool IsSubmitted { get; set; }

        [Column("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("verified_by_user_id")]
        public long? VerifiedByUserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public Student? Student { get; set; }
        public User? VerifiedByUser { get; set; }
    }
}

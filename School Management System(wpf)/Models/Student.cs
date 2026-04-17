using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("students")]
    public class Student : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("user_id")]
        public long UserId { get; set; }
        [Column("profile_image_url")]
        public string? ProfileImageUrl { get; set; }
        [Column("lrn")]
        public string Lrn { get; set; } = string.Empty;
        [Column("student_number")]
        public string StudentNumber { get; set; } = string.Empty;
        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;
        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;
        [Column("middle_name")]
        public string? MiddleName { get; set; }
        [Column("birthdate")]
        public DateTime? Birthdate { get; set; }
        [Column("age")]
        public int? Age { get; set; }
        [Column("sex")]
        public Sex? Sex { get; set; }
        [Column("address")]
        public string? Address { get; set; }
        [Column("contact_no")]
        public string? ContactNo { get; set; }
        [Column("guardian_name")]
        public string? GuardianName { get; set; }
        [Column("guardian_contact")]
        public string? GuardianContact { get; set; }
        [Column("previous_school")]
        public string? PreviousSchool { get; set; }
        [Column("preferred_grade_level_id")]
        public long? PreferredGradeLevelId { get; set; }
        [Column("preferred_curriculum_id")]
        public long? PreferredCurriculumId { get; set; }
        [Column("status")]
        public UserStatus Status { get; set; } = UserStatus.ACTIVE;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public User? User { get; set; }
        public GradeLevel? PreferredGradeLevel { get; set; }
        public Curriculum? PreferredCurriculum { get; set; }
        public List<Enrollment> Enrollments { get; set; } = new();
        public List<ClassStudent> ClassStudents { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("teachers")]
    public class Teacher : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("user_id")]
        public long UserId { get; set; }
        [Column("profile_image_url")]
        public string? ProfileImageUrl { get; set; }
        [Column("employee_no")]
        public string? EmployeeNo { get; set; }
        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;
        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;
        [Column("middle_name")]
        public string? MiddleName { get; set; }
        [Column("email")]
        public string? Email { get; set; }
        [Column("contact_no")]
        public string? ContactNo { get; set; }
        [Column("specialization")]
        public string? Specialization { get; set; }
        [Column("advisory_assignment_status")]
        public string? AdvisoryAssignmentStatus { get; set; }
        [Column("employment_status")]
        public string? EmploymentStatus { get; set; }
        [Column("hire_date")]
        public DateTime? HireDate { get; set; }
        [Column("status")]
        public UserStatus Status { get; set; } = UserStatus.ACTIVE;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public User? User { get; set; }
        public List<Section> AdvisorySections { get; set; } = new();
        public List<ClassOffering> ClassOfferings { get; set; } = new();
    }
}

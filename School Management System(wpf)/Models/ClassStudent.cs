using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("class_students")]
    public class ClassStudent : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("class_offering_id")]
        public long ClassOfferingId { get; set; }
        [Column("student_id")]
        public long StudentId { get; set; }
        [Column("enrollment_id")]
        public long EnrollmentId { get; set; }
        [Column("status")]
        public ClassStudentStatus Status { get; set; } = ClassStudentStatus.ACTIVE;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ClassOffering? ClassOffering { get; set; }
        public Student? Student { get; set; }
        public Enrollment? Enrollment { get; set; }
    }
}

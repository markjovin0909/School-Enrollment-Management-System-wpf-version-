using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("attendance_records")]
    public class AttendanceRecord : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("attendance_session_id")]
        public long AttendanceSessionId { get; set; }
        [Column("student_id")]
        public long StudentId { get; set; }
        [Column("marked_by_user_id")]
        public long? MarkedByUserId { get; set; }
        [Column("status")]
        public AttendanceStatus Status { get; set; } = AttendanceStatus.PRESENT;
        [Column("reason")]
        public string? Reason { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public AttendanceSession? AttendanceSession { get; set; }
        public Student? Student { get; set; }
        public User? MarkedByUser { get; set; }
    }
}

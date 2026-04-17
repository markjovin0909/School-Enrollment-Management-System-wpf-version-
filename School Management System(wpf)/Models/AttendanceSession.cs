using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("attendance_sessions")]
    public class AttendanceSession : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("class_offering_id")]
        public long ClassOfferingId { get; set; }
        [Column("session_date")]
        public DateTime SessionDate { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ClassOffering? ClassOffering { get; set; }
        public List<AttendanceRecord> AttendanceRecords { get; set; } = new();
    }
}

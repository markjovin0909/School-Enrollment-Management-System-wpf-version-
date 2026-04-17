using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("time_slots")]
    public class TimeSlot : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("code")]
        public string Code { get; set; } = string.Empty;
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("start_time")]
        public TimeSpan StartTime { get; set; }
        [Column("end_time")]
        public TimeSpan EndTime { get; set; }
        [Column("is_bell_period")]
        public bool IsBellPeriod { get; set; } = true;
        [Column("sort_order")]
        public int SortOrder { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public List<ClassSchedule> ClassSchedules { get; set; } = new();
    }
}

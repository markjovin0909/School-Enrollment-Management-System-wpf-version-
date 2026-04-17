using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("class_schedules")]
    public class ClassSchedule : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("class_offering_id")]
        public long ClassOfferingId { get; set; }
        [Column("room_id")]
        public long? RoomId { get; set; }
        [Column("time_slot_id")]
        public long? TimeSlotId { get; set; }
        [Column("day_of_week")]
        public byte DayOfWeek { get; set; }
        [Column("start_time")]
        public TimeSpan StartTime { get; set; }
        [Column("end_time")]
        public TimeSpan EndTime { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public ClassOffering? ClassOffering { get; set; }
        public Room? Room { get; set; }
        public TimeSlot? TimeSlot { get; set; }
    }
}

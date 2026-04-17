using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("rooms")]
    public class Room : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("code")]
        public string Code { get; set; } = string.Empty;
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        [Column("capacity")]
        public int? Capacity { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; } = true;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public List<ClassSchedule> ClassSchedules { get; set; } = new();
    }
}

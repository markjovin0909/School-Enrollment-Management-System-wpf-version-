using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("grade_components")]
    public class GradeComponent : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("name")]
        public GradeComponentName Name { get; set; }
        [Column("weight", TypeName = "decimal(6,4)")]
        public decimal Weight { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; } = true;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public List<Assessment> Assessments { get; set; } = new();
    }
}

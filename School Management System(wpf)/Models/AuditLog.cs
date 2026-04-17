using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("audit_logs")]
    public class AuditLog : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("user_id")]
        public long UserId { get; set; }
        [Column("action")]
        public string Action { get; set; } = string.Empty;
        [Column("entity")]
        public string Entity { get; set; } = string.Empty;
        [Column("entity_id")]
        public long? EntityId { get; set; }
        [Column("payload")]
        public string? Payload { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }
    }
}

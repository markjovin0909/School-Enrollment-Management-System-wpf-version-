using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("governed_operation_logs")]
    public class GovernedOperationLog : Interfaces.IBaseModel
    {
        public long Id { get; set; }

        [Column("correlation_id")]
        public string CorrelationId { get; set; } = string.Empty;

        [Column("policy_key")]
        public string PolicyKey { get; set; } = string.Empty;

        [Column("action")]
        public string Action { get; set; } = string.Empty;

        [Column("entity")]
        public string Entity { get; set; } = string.Empty;

        [Column("entity_id")]
        public long? EntityId { get; set; }

        [Column("status")]
        public GovernedOperationStatus Status { get; set; } = GovernedOperationStatus.STARTED;

        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("payload")]
        public string? Payload { get; set; }

        [Column("actor_user_id")]
        public long? ActorUserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("exception_queue_items")]
    public class ExceptionQueueItem : Interfaces.IBaseModel
    {
        public long Id { get; set; }

        [Column("category")]
        public string Category { get; set; } = string.Empty;

        [Column("source_module")]
        public string SourceModule { get; set; } = string.Empty;

        [Column("entity")]
        public string Entity { get; set; } = string.Empty;

        [Column("entity_id")]
        public long? EntityId { get; set; }

        [Column("severity")]
        public ExceptionQueueSeverity Severity { get; set; } = ExceptionQueueSeverity.WARNING;

        [Column("status")]
        public ExceptionQueueStatus Status { get; set; } = ExceptionQueueStatus.OPEN;

        [Column("summary")]
        public string Summary { get; set; } = string.Empty;

        [Column("details")]
        public string? Details { get; set; }

        [Column("correlation_id")]
        public string? CorrelationId { get; set; }

        [Column("assignment_status")]
        public string AssignmentStatus { get; set; } = "UNASSIGNED";

        [Column("assigned_to_user_id")]
        public long? AssignedToUserId { get; set; }

        [Column("acknowledged_by_user_id")]
        public long? AcknowledgedByUserId { get; set; }

        [Column("acknowledged_at")]
        public DateTime? AcknowledgedAt { get; set; }

        [Column("resolved_by_user_id")]
        public long? ResolvedByUserId { get; set; }

        [Column("resolved_at")]
        public DateTime? ResolvedAt { get; set; }

        [Column("occurrence_count")]
        public int OccurrenceCount { get; set; } = 1;

        [Column("last_occurred_at")]
        public DateTime LastOccurredAt { get; set; }

        [Column("created_by_user_id")]
        public long? CreatedByUserId { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

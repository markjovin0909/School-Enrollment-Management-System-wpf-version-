using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("enrollment_state_transitions")]
    public class EnrollmentStateTransition : Interfaces.IBaseModel
    {
        public long Id { get; set; }

        [Column("enrollment_id")]
        public long EnrollmentId { get; set; }

        [Column("previous_status")]
        public EnrollmentStatus? PreviousStatus { get; set; }

        [Column("new_status")]
        public EnrollmentStatus NewStatus { get; set; }

        [Column("previous_approval_status")]
        public EnrollmentApprovalStatus? PreviousApprovalStatus { get; set; }

        [Column("new_approval_status")]
        public EnrollmentApprovalStatus? NewApprovalStatus { get; set; }

        [Column("trigger_action")]
        public EnrollmentTransitionTrigger TriggerAction { get; set; }

        [Column("reason_code")]
        public string? ReasonCode { get; set; }

        [Column("reason_text")]
        public string? ReasonText { get; set; }

        [Column("performed_by_user_id")]
        public long? PerformedByUserId { get; set; }

        [Column("correlation_id")]
        public string? CorrelationId { get; set; }

        [Column("metadata_json")]
        public string? MetadataJson { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}

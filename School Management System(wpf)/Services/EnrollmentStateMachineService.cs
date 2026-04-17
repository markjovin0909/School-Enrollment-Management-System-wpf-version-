using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal sealed class EnrollmentStateMachineService
    {
        private static readonly IReadOnlyDictionary<EnrollmentStatus, HashSet<EnrollmentStatus>> AllowedTransitions =
            new Dictionary<EnrollmentStatus, HashSet<EnrollmentStatus>>
            {
                [EnrollmentStatus.PENDING] = new HashSet<EnrollmentStatus>
                {
                    EnrollmentStatus.ENROLLED,
                    EnrollmentStatus.RESERVED,
                    EnrollmentStatus.CANCELLED,
                    EnrollmentStatus.DROPPED
                },
                [EnrollmentStatus.RESERVED] = new HashSet<EnrollmentStatus>
                {
                    EnrollmentStatus.PENDING,
                    EnrollmentStatus.ENROLLED,
                    EnrollmentStatus.CANCELLED,
                    EnrollmentStatus.DROPPED
                },
                [EnrollmentStatus.ENROLLED] = new HashSet<EnrollmentStatus>
                {
                    EnrollmentStatus.PENDING,
                    EnrollmentStatus.RESERVED,
                    EnrollmentStatus.DROPPED,
                    EnrollmentStatus.COMPLETED,
                    EnrollmentStatus.TRANSFERRED_OUT
                },
                [EnrollmentStatus.CANCELLED] = new HashSet<EnrollmentStatus>(),
                [EnrollmentStatus.DROPPED] = new HashSet<EnrollmentStatus>(),
                [EnrollmentStatus.COMPLETED] = new HashSet<EnrollmentStatus>(),
                [EnrollmentStatus.TRANSFERRED_OUT] = new HashSet<EnrollmentStatus>()
            };

        private static readonly HashSet<EnrollmentStatus> AllowedInitialStatuses = new()
        {
            EnrollmentStatus.PENDING,
            EnrollmentStatus.RESERVED,
            EnrollmentStatus.ENROLLED
        };

        public OperationResult ValidateTransition(EnrollmentStatus? previousStatus, EnrollmentStatus nextStatus)
        {
            if (!previousStatus.HasValue)
            {
                if (AllowedInitialStatuses.Contains(nextStatus))
                {
                    return OperationResult.Ok();
                }

                return OperationResult.Fail($"Invalid initial enrollment status '{nextStatus}'.");
            }

            if (previousStatus.Value == nextStatus)
            {
                return OperationResult.Ok();
            }

            if (!AllowedTransitions.TryGetValue(previousStatus.Value, out var allowed) || !allowed.Contains(nextStatus))
            {
                return OperationResult.Fail($"Forbidden enrollment transition: {previousStatus} -> {nextStatus}.");
            }

            return OperationResult.Ok();
        }

        public OperationResult<EnrollmentStateTransition?> RecordTransition(
            AppDbContext db,
            long enrollmentId,
            EnrollmentStatus? previousStatus,
            EnrollmentStatus nextStatus,
            EnrollmentApprovalStatus? previousApprovalStatus,
            EnrollmentApprovalStatus? nextApprovalStatus,
            EnrollmentTransitionTrigger trigger,
            string? reasonCode = null,
            string? reasonText = null,
            object? metadata = null,
            long? actorUserId = null)
        {
            var validation = ValidateTransition(previousStatus, nextStatus);
            if (!validation.Success)
            {
                return OperationResult<EnrollmentStateTransition?>.Fail(validation.Message, validation.Errors);
            }

            if (previousStatus.HasValue &&
                previousStatus.Value == nextStatus &&
                previousApprovalStatus == nextApprovalStatus)
            {
                return OperationResult<EnrollmentStateTransition?>.Ok(null, "No transition persisted because state is unchanged.");
            }

            StructuralSchemaService.EnsureApplied(db);
            var transition = new EnrollmentStateTransition
            {
                EnrollmentId = enrollmentId,
                PreviousStatus = previousStatus,
                NewStatus = nextStatus,
                PreviousApprovalStatus = previousApprovalStatus,
                NewApprovalStatus = nextApprovalStatus,
                TriggerAction = trigger,
                ReasonCode = string.IsNullOrWhiteSpace(reasonCode) ? "UNSPECIFIED" : reasonCode.Trim(),
                ReasonText = string.IsNullOrWhiteSpace(reasonText) ? null : reasonText.Trim(),
                PerformedByUserId = actorUserId ?? SessionContext.CurrentUser?.Id,
                CorrelationId = CorrelationContext.Ensure(),
                MetadataJson = metadata == null ? null : JsonSerializer.Serialize(metadata),
                CreatedAt = DateTime.UtcNow
            };

            db.EnrollmentStateTransitions.Add(transition);
            db.SaveChanges();
            return OperationResult<EnrollmentStateTransition?>.Ok(transition, "Enrollment transition recorded.");
        }

        public IReadOnlyList<EnrollmentStateTransition> GetHistory(long enrollmentId)
        {
            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            return db.EnrollmentStateTransitions
                .Where(x => x.EnrollmentId == enrollmentId)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToList();
        }
    }
}

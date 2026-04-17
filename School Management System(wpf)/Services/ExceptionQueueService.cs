using System;
using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal sealed class ExceptionQueueService
    {
        public ExceptionQueueItem Raise(ExceptionQueueCreateRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);

            var summary = string.IsNullOrWhiteSpace(request.Summary) ? "Unhandled exception case." : request.Summary.Trim();
            var sourceModule = string.IsNullOrWhiteSpace(request.SourceModule) ? "unknown" : request.SourceModule.Trim();
            var category = string.IsNullOrWhiteSpace(request.Category) ? "GENERAL" : request.Category.Trim().ToUpperInvariant();
            var entity = string.IsNullOrWhiteSpace(request.Entity) ? "unknown" : request.Entity.Trim().ToLowerInvariant();

            var existingOpen = db.ExceptionQueueItems
                .Where(x =>
                    x.Status != ExceptionQueueStatus.RESOLVED &&
                    x.Status != ExceptionQueueStatus.DISMISSED &&
                    x.Category == category &&
                    x.SourceModule == sourceModule &&
                    x.Entity == entity &&
                    x.EntityId == request.EntityId &&
                    x.Summary == summary)
                .OrderByDescending(x => x.UpdatedAt)
                .FirstOrDefault();

            if (existingOpen != null)
            {
                existingOpen.OccurrenceCount = Math.Max(1, existingOpen.OccurrenceCount) + 1;
                existingOpen.LastOccurredAt = DateTime.UtcNow;
                existingOpen.UpdatedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(request.Details))
                {
                    existingOpen.Details = request.Details.Trim();
                }

                if (string.IsNullOrWhiteSpace(existingOpen.CorrelationId))
                {
                    existingOpen.CorrelationId = request.CorrelationId ?? CorrelationContext.CurrentId;
                }

                db.SaveChanges();
                return existingOpen;
            }

            var now = DateTime.UtcNow;
            var item = new ExceptionQueueItem
            {
                Category = category,
                SourceModule = sourceModule,
                Entity = entity,
                EntityId = request.EntityId,
                Severity = request.Severity,
                Status = ExceptionQueueStatus.OPEN,
                Summary = summary,
                Details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim(),
                CorrelationId = request.CorrelationId ?? CorrelationContext.CurrentId,
                AssignmentStatus = "UNASSIGNED",
                OccurrenceCount = 1,
                LastOccurredAt = now,
                CreatedByUserId = request.CreatedByUserId ?? SessionContext.CurrentUser?.Id,
                CreatedAt = now,
                UpdatedAt = now
            };

            db.ExceptionQueueItems.Add(item);
            db.SaveChanges();
            return item;
        }

        public IReadOnlyList<ExceptionQueueItem> GetActive(int take = 200)
        {
            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);

            return db.ExceptionQueueItems
                .Where(x => x.Status != ExceptionQueueStatus.RESOLVED && x.Status != ExceptionQueueStatus.DISMISSED)
                .OrderByDescending(x => x.UpdatedAt)
                .Take(Math.Max(1, take))
                .ToList();
        }

        public OperationResult<ExceptionQueueItem> Assign(long id, long assignedToUserId, long? actorUserId = null)
        {
            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            var item = db.ExceptionQueueItems.Find(id);
            if (item == null)
            {
                return OperationResult<ExceptionQueueItem>.Fail("Exception queue item not found.");
            }

            item.AssignedToUserId = assignedToUserId;
            item.AssignmentStatus = "ASSIGNED";
            item.Status = ExceptionQueueStatus.ASSIGNED;
            item.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();

            AuditTrailService.LogWithActor(actorUserId, "EXCEPTION_ASSIGN", "exception_queue_items", item.Id, null, new
            {
                item.AssignedToUserId,
                item.Status,
                item.AssignmentStatus
            });

            return OperationResult<ExceptionQueueItem>.Ok(item, "Exception queue item assigned.");
        }

        public OperationResult<ExceptionQueueItem> Resolve(long id, string resolutionSummary, long? actorUserId = null)
        {
            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            var item = db.ExceptionQueueItems.Find(id);
            if (item == null)
            {
                return OperationResult<ExceptionQueueItem>.Fail("Exception queue item not found.");
            }

            item.Status = ExceptionQueueStatus.RESOLVED;
            item.AssignmentStatus = "RESOLVED";
            item.ResolvedByUserId = actorUserId ?? SessionContext.CurrentUser?.Id;
            item.ResolvedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(resolutionSummary))
            {
                item.Details = $"{item.Details ?? string.Empty}\nRESOLUTION: {resolutionSummary.Trim()}".Trim();
            }

            db.SaveChanges();
            AuditTrailService.LogWithActor(actorUserId, "EXCEPTION_RESOLVE", "exception_queue_items", item.Id, null, new
            {
                item.Status,
                item.AssignmentStatus,
                item.ResolvedByUserId,
                item.ResolvedAt
            });
            return OperationResult<ExceptionQueueItem>.Ok(item, "Exception queue item resolved.");
        }
    }

    internal sealed class ExceptionQueueCreateRequest
    {
        public string Category { get; set; } = string.Empty;
        public string SourceModule { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public long? EntityId { get; set; }
        public ExceptionQueueSeverity Severity { get; set; } = ExceptionQueueSeverity.WARNING;
        public string Summary { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? CorrelationId { get; set; }
        public long? CreatedByUserId { get; set; }
    }
}

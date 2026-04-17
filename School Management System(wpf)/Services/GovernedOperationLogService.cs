using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal sealed class GovernedOperationLogService
    {
        public void Log(
            PolicyActionKey policyKey,
            string action,
            string entity,
            long? entityId,
            GovernedOperationStatus status,
            string message,
            object? payload = null,
            long? actorUserId = null,
            string? correlationId = null)
        {
            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);

            db.GovernedOperationLogs.Add(new GovernedOperationLog
            {
                CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? CorrelationContext.Ensure() : correlationId!,
                PolicyKey = policyKey.ToString(),
                Action = string.IsNullOrWhiteSpace(action) ? policyKey.ToString() : action.Trim(),
                Entity = string.IsNullOrWhiteSpace(entity) ? "unknown" : entity.Trim().ToLowerInvariant(),
                EntityId = entityId,
                Status = status,
                Message = string.IsNullOrWhiteSpace(message) ? status.ToString() : message.Trim(),
                Payload = payload == null ? null : JsonSerializer.Serialize(payload),
                ActorUserId = actorUserId ?? SessionContext.CurrentUser?.Id,
                CreatedAt = DateTime.UtcNow
            });

            db.SaveChanges();
        }

        public IReadOnlyList<GovernedOperationLog> GetRecent(int take = 120)
        {
            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);

            return db.GovernedOperationLogs
                .OrderByDescending(x => x.CreatedAt)
                .Take(Math.Max(1, take))
                .ToList();
        }
    }
}

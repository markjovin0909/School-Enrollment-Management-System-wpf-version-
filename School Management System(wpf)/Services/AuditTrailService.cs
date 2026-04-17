using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal static class AuditTrailService
    {
        private static readonly HashSet<string> GovernedEnrollmentActions = new(StringComparer.OrdinalIgnoreCase)
        {
            "APPROVE",
            "RETURN_FOR_CORRECTION",
            "CANCEL",
            "DROP",
            "SET_STATUS",
            "PROMOTE_WAITLIST"
        };

        public static void Log(string action, string entity, long? entityId, object? oldData = null, object? newData = null)
        {
            LogWithActor(SessionContext.CurrentUser?.Id, action, entity, entityId, oldData, newData);
        }

        public static void LogWithActor(long? actorUserId, string action, string entity, long? entityId, object? oldData = null, object? newData = null)
        {
            var normalizedAction = string.IsNullOrWhiteSpace(action) ? "UNKNOWN" : action.Trim().ToUpperInvariant();
            var normalizedEntity = string.IsNullOrWhiteSpace(entity) ? "unknown" : entity.Trim().ToLowerInvariant();
            var userId = actorUserId ?? SessionContext.CurrentUser?.Id ?? 0;
            var correlationId = CorrelationContext.Ensure();
            var validation = ValidateGovernedPayload(normalizedAction, normalizedEntity, entityId, newData);
            var payload = JsonSerializer.Serialize(new
            {
                old = oldData,
                @new = newData,
                governance = new
                {
                    CorrelationId = correlationId,
                    validation.IsGovernedAction,
                    validation.PayloadValid,
                    Issues = validation.Issues
                }
            });

            var svc = new AuditLogService();
            svc.Create(new AuditLog
            {
                UserId = userId,
                Action = normalizedAction,
                Entity = normalizedEntity,
                EntityId = entityId,
                Payload = payload,
                CreatedAt = DateTime.UtcNow
            });
        }

        private static AuditPayloadValidation ValidateGovernedPayload(string action, string entity, long? entityId, object? newData)
        {
            var isGoverned = string.Equals(entity, "enrollments", StringComparison.OrdinalIgnoreCase)
                             && GovernedEnrollmentActions.Contains(action);
            if (!isGoverned)
            {
                return AuditPayloadValidation.NotGoverned;
            }

            var issues = new List<string>();
            if (newData == null)
            {
                issues.Add("NEW_DATA_REQUIRED");
                return new AuditPayloadValidation(true, false, issues);
            }

            JsonDocument? doc = null;
            try
            {
                doc = JsonDocument.Parse(JsonSerializer.Serialize(newData));
                var root = doc.RootElement;

                if (!HasReasonCode(root))
                {
                    issues.Add("REASON_CODE_REQUIRED");
                }

                if (string.Equals(action, "PROMOTE_WAITLIST", StringComparison.OrdinalIgnoreCase))
                {
                    if (!HasNumericProperty(root, "SchoolYearId"))
                    {
                        issues.Add("SCHOOL_YEAR_REQUIRED");
                    }

                    if (!HasNumericProperty(root, "SectionId"))
                    {
                        issues.Add("SECTION_REQUIRED");
                    }
                }
                else
                {
                    if (!entityId.HasValue || entityId.Value <= 0)
                    {
                        issues.Add("ENTITY_ID_REQUIRED");
                    }

                    if (!HasStringProperty(root, "PreviousStatus"))
                    {
                        issues.Add("PREVIOUS_STATUS_REQUIRED");
                    }

                    if (!HasStringProperty(root, "NewStatus"))
                    {
                        issues.Add("NEW_STATUS_REQUIRED");
                    }
                }
            }
            catch
            {
                issues.Add("NEW_DATA_SERIALIZATION_ERROR");
            }
            finally
            {
                doc?.Dispose();
            }

            return new AuditPayloadValidation(true, issues.Count == 0, issues);
        }

        private static bool HasReasonCode(JsonElement root)
        {
            if (HasStringProperty(root, "ReasonCode"))
            {
                return true;
            }

            if (TryGetPropertyIgnoreCase(root, "Reason", out var reasonNode))
            {
                return HasStringProperty(reasonNode, "Code") || HasStringProperty(reasonNode, "ReasonCode");
            }

            return false;
        }

        private static bool HasStringProperty(JsonElement root, string propertyName)
        {
            if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                var value = property.GetString();
                return !string.IsNullOrWhiteSpace(value);
            }

            return property.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False;
        }

        private static bool HasNumericProperty(JsonElement root, string propertyName)
        {
            if (!TryGetPropertyIgnoreCase(root, propertyName, out var property))
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.Number)
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                return long.TryParse(property.GetString(), out _);
            }

            return false;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement root, string propertyName, out JsonElement value)
        {
            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private sealed class AuditPayloadValidation
        {
            public static AuditPayloadValidation NotGoverned { get; } = new(false, true, Array.Empty<string>());

            public AuditPayloadValidation(bool isGovernedAction, bool payloadValid, IEnumerable<string> issues)
            {
                IsGovernedAction = isGovernedAction;
                PayloadValid = payloadValid;
                Issues = (issues ?? Array.Empty<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToArray();
            }

            public bool IsGovernedAction { get; }
            public bool PayloadValid { get; }
            public string[] Issues { get; }
        }
    }
}

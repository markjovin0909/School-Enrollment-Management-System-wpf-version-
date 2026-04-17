using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal sealed class NotificationCenterService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public void PublishToRole(UserRole role, string title, string message, string category, long? entityId = null)
        {
            var entry = new NotificationEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow,
                Role = role,
                Title = title,
                Message = message,
                Category = category,
                EntityId = entityId
            };
            Append(entry);
        }

        public void PublishToUser(long userId, string title, string message, string category, long? entityId = null)
        {
            var entry = new NotificationEntry
            {
                Id = Guid.NewGuid().ToString("N"),
                CreatedAtUtc = DateTime.UtcNow,
                UserId = userId,
                Title = title,
                Message = message,
                Category = category,
                EntityId = entityId
            };
            Append(entry);
        }

        public IReadOnlyList<NotificationEntry> GetForUser(User user, bool includeRead = true, int take = 100)
        {
            var all = ReadAll();
            IEnumerable<NotificationEntry> filtered = all
                .Where(x =>
                    (x.UserId.HasValue && x.UserId.Value == user.Id) ||
                    (x.Role.HasValue && x.Role.Value == user.Role));

            if (!includeRead)
            {
                filtered = filtered.Where(x => !x.ReadByUserIds.Contains(user.Id));
            }

            return filtered
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(Math.Max(1, take))
                .ToList();
        }

        public int GetUnreadCount(User user)
        {
            return GetForUser(user, includeRead: false, take: 2000).Count;
        }

        public void MarkAllAsRead(User user)
        {
            var all = ReadAll();
            var changed = false;

            foreach (var entry in all)
            {
                var visible =
                    (entry.UserId.HasValue && entry.UserId.Value == user.Id) ||
                    (entry.Role.HasValue && entry.Role.Value == user.Role);
                if (!visible || entry.ReadByUserIds.Contains(user.Id))
                {
                    continue;
                }

                entry.ReadByUserIds.Add(user.Id);
                changed = true;
            }

            if (changed)
            {
                SaveAll(all);
            }
        }

        public void PublishFromAudit(string action, string entity, long? entityId, string? payload)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return;
            }

            var normalizedAction = action.Trim().ToUpperInvariant();
            var normalizedEntity = (entity ?? string.Empty).Trim().ToLowerInvariant();

            if (normalizedAction == "BACKUP_FAILED" || normalizedAction == "RESTORE_FAILED")
            {
                PublishToRole(
                    UserRole.SUPERADMIN,
                    $"Database {normalizedAction.Replace('_', ' ')}",
                    BuildAuditMessage(normalizedAction, normalizedEntity, payload),
                    "Database",
                    entityId);
                return;
            }

            if (normalizedAction == "BACKUP_SUCCESS" || normalizedAction == "RESTORE_SUCCESS")
            {
                PublishToRole(
                    UserRole.SUPERADMIN,
                    $"Database {normalizedAction.Replace('_', ' ')}",
                    BuildAuditMessage(normalizedAction, normalizedEntity, payload),
                    "Database",
                    entityId);
                return;
            }

            if (normalizedEntity == "announcements" && normalizedAction is "CREATE" or "UPDATE")
            {
                PublishToRole(
                    UserRole.STUDENT,
                    "Announcement update",
                    "A new or updated announcement is available.",
                    "Announcements",
                    entityId);
            }
        }

        private static string BuildAuditMessage(string action, string entity, string? payload)
        {
            var baseText = $"{action.Replace('_', ' ')} on {entity}.";
            if (string.IsNullOrWhiteSpace(payload))
            {
                return baseText;
            }

            var compact = payload.Replace(Environment.NewLine, " ").Trim();
            if (compact.Length > 220)
            {
                compact = compact.Substring(0, 220).TrimEnd() + "...";
            }

            return $"{baseText} {compact}";
        }

        private void Append(NotificationEntry entry)
        {
            try
            {
                var all = ReadAll();
                all.Add(entry);
                all = all
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .Take(1000)
                    .ToList();
                SaveAll(all);
            }
            catch
            {
                // Notification persistence must never block business flow.
            }
        }

        private List<NotificationEntry> ReadAll()
        {
            var path = GetStoragePath();
            if (!File.Exists(path))
            {
                return new List<NotificationEntry>();
            }

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize<List<NotificationEntry>>(json, JsonOptions) ?? new List<NotificationEntry>();
            }
            catch
            {
                return new List<NotificationEntry>();
            }
        }

        private void SaveAll(List<NotificationEntry> notifications)
        {
            var path = GetStoragePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(notifications, JsonOptions);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private static string GetStoragePath()
        {
            var appName = string.IsNullOrWhiteSpace(Application.ProductName)
                ? "School Management System"
                : Application.ProductName.Trim();
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                appName,
                "Notifications");
            Directory.CreateDirectory(root);
            return Path.Combine(root, "notifications.json");
        }
    }

    internal sealed class NotificationEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public long? UserId { get; set; }
        public UserRole? Role { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public long? EntityId { get; set; }
        public List<long> ReadByUserIds { get; set; } = new();
    }
}

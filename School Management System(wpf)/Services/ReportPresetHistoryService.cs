using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace School_Management_System.Services
{
    internal sealed class ReportPresetHistoryService
    {
        private const int MaxHistoryEntries = 300;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public IReadOnlyList<ReportFilterPreset> LoadPresets()
        {
            return ReadPresets()
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.CreatedAtUtc)
                .ToList();
        }

        public OperationResult<ReportFilterPreset> SavePreset(ReportFilterPreset preset)
        {
            if (preset == null)
            {
                return OperationResult<ReportFilterPreset>.Fail("Preset is required.");
            }

            var trimmedName = (preset.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return OperationResult<ReportFilterPreset>.Fail("Preset name is required.");
            }

            try
            {
                var presets = ReadPresets();
                var now = DateTime.UtcNow;

                var existingById = !string.IsNullOrWhiteSpace(preset.Id)
                    ? presets.FirstOrDefault(x => string.Equals(x.Id, preset.Id, StringComparison.OrdinalIgnoreCase))
                    : null;
                var existingByName = presets.FirstOrDefault(x =>
                    string.Equals(x.Name, trimmedName, StringComparison.OrdinalIgnoreCase) &&
                    (existingById == null || !string.Equals(x.Id, existingById.Id, StringComparison.OrdinalIgnoreCase)));

                ReportFilterPreset target;
                if (existingById != null)
                {
                    target = existingById;
                }
                else if (existingByName != null)
                {
                    target = existingByName;
                }
                else
                {
                    target = new ReportFilterPreset
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        CreatedAtUtc = now
                    };
                    presets.Add(target);
                }

                target.Name = trimmedName;
                target.ReportType = preset.ReportType ?? string.Empty;
                target.SchoolYearId = preset.SchoolYearId;
                target.GradeId = preset.GradeId;
                target.SectionId = preset.SectionId;
                target.Status = preset.Status ?? "All";
                target.StudentId = preset.StudentId;
                target.UpdatedAtUtc = now;
                target.CreatedByUserId = preset.CreatedByUserId;
                target.CreatedByUsername = preset.CreatedByUsername ?? string.Empty;

                WritePresets(presets);
                return OperationResult<ReportFilterPreset>.Ok(target, "Preset saved.");
            }
            catch (Exception ex)
            {
                return OperationResult<ReportFilterPreset>.Fail($"Failed to save preset: {ex.Message}");
            }
        }

        public OperationResult DeletePreset(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                return OperationResult.Fail("Preset is required.");
            }

            try
            {
                var presets = ReadPresets();
                var removed = presets.RemoveAll(x => string.Equals(x.Id, presetId, StringComparison.OrdinalIgnoreCase));
                if (removed == 0)
                {
                    return OperationResult.Fail("Preset not found.");
                }

                WritePresets(presets);
                return OperationResult.Ok("Preset deleted.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Failed to delete preset: {ex.Message}");
            }
        }

        public IReadOnlyList<ReportRunHistoryEntry> LoadHistory(int take = 60)
        {
            return ReadHistory()
                .OrderByDescending(x => x.TimestampUtc)
                .Take(Math.Max(1, take))
                .ToList();
        }

        public void AppendHistory(ReportRunHistoryEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            try
            {
                var history = ReadHistory();
                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    entry.Id = Guid.NewGuid().ToString("N");
                }

                if (entry.TimestampUtc == default)
                {
                    entry.TimestampUtc = DateTime.UtcNow;
                }

                history.Add(entry);
                history = history
                    .OrderByDescending(x => x.TimestampUtc)
                    .Take(MaxHistoryEntries)
                    .ToList();
                WriteHistory(history);
            }
            catch
            {
                // History persistence should not block reporting workflows.
            }
        }

        private static List<ReportFilterPreset> ReadPresets()
        {
            var path = GetPresetsFilePath();
            if (!File.Exists(path))
            {
                return new List<ReportFilterPreset>();
            }

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize<List<ReportFilterPreset>>(json, JsonOptions) ?? new List<ReportFilterPreset>();
            }
            catch
            {
                return new List<ReportFilterPreset>();
            }
        }

        private static void WritePresets(List<ReportFilterPreset> presets)
        {
            var path = GetPresetsFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(presets, JsonOptions);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private static List<ReportRunHistoryEntry> ReadHistory()
        {
            var path = GetHistoryFilePath();
            if (!File.Exists(path))
            {
                return new List<ReportRunHistoryEntry>();
            }

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize<List<ReportRunHistoryEntry>>(json, JsonOptions) ?? new List<ReportRunHistoryEntry>();
            }
            catch
            {
                return new List<ReportRunHistoryEntry>();
            }
        }

        private static void WriteHistory(List<ReportRunHistoryEntry> history)
        {
            var path = GetHistoryFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.Serialize(history, JsonOptions);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private static string GetReportsDataDirectory()
        {
            var appName = string.IsNullOrWhiteSpace(Application.ProductName)
                ? "School Management System"
                : Application.ProductName.Trim();
            var root = Path.Combine(
                GetApplicationDataRoot(),
                appName,
                "Reports");
            Directory.CreateDirectory(root);
            return root;
        }

        private static string GetApplicationDataRoot()
        {
            var overridden = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrWhiteSpace(overridden))
            {
                return overridden.Trim();
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        private static string GetPresetsFilePath()
        {
            return Path.Combine(GetReportsDataDirectory(), "presets.json");
        }

        private static string GetHistoryFilePath()
        {
            return Path.Combine(GetReportsDataDirectory(), "run_history.json");
        }
    }

    internal sealed class ReportFilterPreset
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public long? SchoolYearId { get; set; }
        public long? GradeId { get; set; }
        public long? SectionId { get; set; }
        public string Status { get; set; } = "All";
        public long? StudentId { get; set; }
        public long? CreatedByUserId { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    internal sealed class ReportRunHistoryEntry
    {
        public string Id { get; set; } = string.Empty;
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string Action { get; set; } = "RUN";
        public string ReportType { get; set; } = string.Empty;
        public string PresetName { get; set; } = string.Empty;
        public long? SchoolYearId { get; set; }
        public long? GradeId { get; set; }
        public long? SectionId { get; set; }
        public string Status { get; set; } = "All";
        public long? StudentId { get; set; }
        public int RowCount { get; set; }
        public bool Success { get; set; } = true;
        public string Notes { get; set; } = string.Empty;
        public string ExportFilePath { get; set; } = string.Empty;
        public long? ExecutedByUserId { get; set; }
        public string ExecutedByUsername { get; set; } = string.Empty;
    }
}

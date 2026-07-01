using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal sealed class BackupRestoreService
    {
        private const int MaxHistoryEntries = 200;
        private const string BrandingAssetsFolderName = "branding-assets";
        private readonly PermissionBoundaryService _permissionBoundary = new();
        private readonly GovernedOperationLogService _operationLogService = new();
        private readonly ExceptionQueueService _exceptionQueueService = new();
        private readonly PreflightPipelineService _preflightPipeline = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public BackupRestoreSettings LoadSettings()
        {
            var path = GetSettingsFilePath();
            if (!File.Exists(path))
            {
                return new BackupRestoreSettings
                {
                    BackupFolder = GetDefaultBackupFolder(),
                    ZipCompression = false
                };
            }

            try
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize<BackupRestoreSettings>(json, JsonOptions) ?? new BackupRestoreSettings();
                if (string.IsNullOrWhiteSpace(settings.BackupFolder))
                {
                    settings.BackupFolder = GetDefaultBackupFolder();
                }

                return settings;
            }
            catch
            {
                return new BackupRestoreSettings
                {
                    BackupFolder = GetDefaultBackupFolder(),
                    ZipCompression = false
                };
            }
        }

        public void SaveSettings(BackupRestoreSettings settings)
        {
            settings.BackupFolder = string.IsNullOrWhiteSpace(settings.BackupFolder)
                ? GetDefaultBackupFolder()
                : settings.BackupFolder.Trim();

            var path = GetSettingsFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions), Encoding.UTF8);
        }

        public IReadOnlyList<BackupRestoreHistoryEntry> LoadHistory(int take = 30)
        {
            var history = ReadHistoryCore();
            return history
                .OrderByDescending(x => x.TimestampUtc)
                .Take(Math.Max(1, take))
                .ToList();
        }

        public RestorePreflightResult EvaluateRestorePreflight(RestoreRequest request)
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var correlationId = CorrelationContext.Ensure();

            if (request == null)
            {
                var fail = RestorePreflightResult.Fail("Restore request is required.");
                fail.Checks.Add(PreflightCheckResult.Block("REQUEST_REQUIRED", "Restore request is required."));
                RaiseRestorePreflightException(fail, correlationId);
                return fail;
            }

            var path = request.RestoreFilePath?.Trim() ?? string.Empty;
            string? sqlPath = null;
            string? tempExtractDir = null;

            try
            {
                var baseChecks = _preflightPipeline.Evaluate("Restore", new Func<PreflightCheckResult>[]
                {
                    () => string.IsNullOrWhiteSpace(path)
                        ? PreflightCheckResult.Block("RESTORE_FILE_REQUIRED", "Select a restore file.")
                        : PreflightCheckResult.Pass("RESTORE_FILE_REQUIRED", "Restore file was provided."),
                    () => !string.IsNullOrWhiteSpace(path) && File.Exists(path)
                        ? PreflightCheckResult.Pass("RESTORE_FILE_EXISTS", "Restore file exists.")
                        : PreflightCheckResult.Block("RESTORE_FILE_EXISTS", "Restore file does not exist.")
                });

                if (!baseChecks.Success)
                {
                    var baseFail = RestorePreflightResult.Fail(baseChecks.Summary, baseChecks.BlockingReasons, baseChecks.Warnings);
                    baseFail.Checks.AddRange(baseChecks.Checks);
                    RaiseRestorePreflightException(baseFail, correlationId);
                    return baseFail;
                }

                if (!ValidateBackupArtifact(path, out var artifactMessage))
                {
                    var fail = RestorePreflightResult.Fail(
                        "Restore artifact validation failed.",
                        new[] { artifactMessage });
                    fail.Checks.AddRange(baseChecks.Checks);
                    fail.Checks.Add(PreflightCheckResult.Block("RESTORE_ARTIFACT_VALID", artifactMessage));
                    RaiseRestorePreflightException(fail, correlationId);
                    return fail;
                }

                var extension = Path.GetExtension(path);
                if (string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    tempExtractDir = Path.Combine(Path.GetTempPath(), $"sms_restore_preflight_{Guid.NewGuid():N}");
                    Directory.CreateDirectory(tempExtractDir);
                    ZipFile.ExtractToDirectory(path, tempExtractDir, overwriteFiles: true);
                    sqlPath = Directory.GetFiles(tempExtractDir, "*.sql", SearchOption.AllDirectories)
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .FirstOrDefault();
                }
                else if (string.Equals(extension, ".sql", StringComparison.OrdinalIgnoreCase))
                {
                    sqlPath = path;
                }

                var compatibilityChecks = new List<Func<PreflightCheckResult>>();
                compatibilityChecks.Add(() =>
                    string.IsNullOrWhiteSpace(sqlPath)
                        ? PreflightCheckResult.Block("RESTORE_SQL_SOURCE", "Only .sql or .zip restore artifacts containing SQL are supported.")
                        : PreflightCheckResult.Pass("RESTORE_SQL_SOURCE", "SQL source file resolved."));

                var discoveredTables = string.IsNullOrWhiteSpace(sqlPath)
                    ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    : DiscoverSqlTables(sqlPath);

                var requiredTables = new[] { "users", "students", "teachers", "enrollments" };
                var optionalTables = new[] { "school_settings", "student_requirements", "archive_records" };
                var missingRequired = requiredTables.Where(table => !discoveredTables.Contains(table)).ToList();
                var missingOptional = optionalTables.Where(table => !discoveredTables.Contains(table)).ToList();

                compatibilityChecks.Add(() =>
                    missingRequired.Count == 0
                        ? PreflightCheckResult.Pass("RESTORE_REQUIRED_TABLES", "Required core tables detected.")
                        : PreflightCheckResult.Block("RESTORE_REQUIRED_TABLES", $"Missing required tables: {string.Join(", ", missingRequired)}."));

                compatibilityChecks.Add(() =>
                    missingOptional.Count == 0
                        ? PreflightCheckResult.Pass("RESTORE_OPTIONAL_TABLES", "Optional compatibility tables detected.")
                        : PreflightCheckResult.Warning("RESTORE_OPTIONAL_TABLES", $"Optional tables not detected: {string.Join(", ", missingOptional)}."));

                compatibilityChecks.Add(() =>
                {
                    var fileInfo = new FileInfo(path);
                    return fileInfo.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-60)
                        ? PreflightCheckResult.Warning("RESTORE_FILE_AGE", "Restore artifact is older than 60 days. Verify this is the intended snapshot.")
                        : PreflightCheckResult.Pass("RESTORE_FILE_AGE", "Restore artifact recency check passed.");
                });

                compatibilityChecks.Add(() =>
                    TryResolveMySqlTools(request.MySqlBinFolder, out _, out _, out var mysqlToolsMessage)
                        ? PreflightCheckResult.Pass("MYSQL_TOOLING", "MySQL tools resolved.")
                        : PreflightCheckResult.Warning("MYSQL_TOOLING", mysqlToolsMessage));

                compatibilityChecks.Add(() =>
                    string.Equals(extension, ".sql", StringComparison.OrdinalIgnoreCase)
                        ? PreflightCheckResult.Warning("RESTORE_BRANDING_ASSETS", "Plain SQL backup selected. Branding assets restore only if a companion branding-assets folder exists beside the SQL file.")
                        : PreflightCheckResult.Pass("RESTORE_BRANDING_ASSETS", "ZIP restore artifact can carry branding assets."));

                var compatibility = _preflightPipeline.Evaluate("Restore", compatibilityChecks);
                var checks = baseChecks.Checks.Concat(compatibility.Checks).ToList();
                if (!compatibility.Success)
                {
                    var fail = RestorePreflightResult.Fail(
                        "Restore preflight failed due to compatibility blockers.",
                        compatibility.BlockingReasons,
                        compatibility.Warnings);
                    fail.Checks.AddRange(checks);
                    RaiseRestorePreflightException(fail, correlationId);
                    return fail;
                }

                var summary = compatibility.Warnings.Count == 0
                    ? $"Restore preflight passed. Core compatibility checks passed ({discoveredTables.Count} tables detected)."
                    : $"Restore preflight passed with warnings ({discoveredTables.Count} tables detected).";
                var ok = RestorePreflightResult.Ok(summary, compatibility.Warnings);
                ok.Checks.AddRange(checks);
                return ok;
            }
            catch (Exception ex)
            {
                var fail = RestorePreflightResult.Fail("Restore preflight failed.", new[] { $"Preflight error: {ex.Message}" });
                fail.Checks.Add(PreflightCheckResult.Block("PREFLIGHT_RUNTIME_ERROR", ex.Message));
                RaiseRestorePreflightException(fail, correlationId);
                return fail;
            }
            finally
            {
                TryDeleteDirectory(tempExtractDir);
            }
        }

        public string? GetLatestLogFilePath()
        {
            var logsDir = GetLogsDirectory();
            if (!Directory.Exists(logsDir))
            {
                return null;
            }

            var latest = Directory
                .GetFiles(logsDir, "*.log", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(info => info.LastWriteTimeUtc)
                .FirstOrDefault();

            return latest?.FullName;
        }

        private void RaiseRestorePreflightException(RestorePreflightResult result, string correlationId)
        {
            if (result == null || result.Success)
            {
                return;
            }

            try
            {
                _exceptionQueueService.Raise(new ExceptionQueueCreateRequest
                {
                    Category = "PREFLIGHT_FAILURE",
                    SourceModule = "BackupRestore.RestorePreflight",
                    Entity = "database_backup_restore",
                    Severity = ExceptionQueueSeverity.CRITICAL,
                    Summary = "Restore preflight blocked.",
                    Details = string.Join(Environment.NewLine, result.BlockingReasons.Concat(result.Warnings)),
                    CorrelationId = correlationId
                });
            }
            catch
            {
                // Exception queue recording must not block restore preflight checks.
            }
        }

        public async Task<BackupRestoreExecutionResult> BackupAsync(
            BackupRequest request,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var correlationId = CorrelationContext.Ensure();
            var result = new BackupRestoreExecutionResult
            {
                Action = "BACKUP",
                Success = false,
                Summary = "Backup failed."
            };

            var details = new StringBuilder();
            var startedAt = DateTime.UtcNow;
            string? optionFilePath = null;
            string backupPath = string.Empty;

            try
            {
                _permissionBoundary.EnsureAllowed(PolicyActionKey.MAINTENANCE_BACKUP_EXECUTE);
                _operationLogService.Log(
                    PolicyActionKey.MAINTENANCE_BACKUP_EXECUTE,
                    "BACKUP_START",
                    "database_backup_restore",
                    null,
                    GovernedOperationStatus.STARTED,
                    "Database backup started.",
                    payload: new { request?.BackupFolder, request?.ZipCompression },
                    correlationId: correlationId);

                if (request == null)
                {
                    result.Summary = "Backup request is required.";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.BackupFolder))
                {
                    result.Summary = "Backup folder is required.";
                    return result;
                }

                Directory.CreateDirectory(request.BackupFolder);
                progress?.Report("Resolving MySQL tools...");

                if (!TryResolveMySqlTools(request.MySqlBinFolder, out var mysqldumpPath, out var mysqlPath, out var resolveMessage))
                {
                    result.Summary = resolveMessage;
                    details.AppendLine(resolveMessage);
                    return result;
                }

                _ = mysqlPath;
                details.AppendLine($"Using mysqldump: {mysqldumpPath}");

                var connection = GetConnectionSettings();
                var fileNameBase = $"{connection.Database}_backup_{DateTime.Now:yyyyMMdd_HHmmss}";
                var sqlPath = Path.Combine(request.BackupFolder, $"{fileNameBase}.sql");
                backupPath = sqlPath;

                optionFilePath = CreateOptionFile(connection);
                progress?.Report("Running mysqldump...");

                var args = string.Join(" ", new[]
                {
                    $"--defaults-extra-file=\"{optionFilePath}\"",
                    "--single-transaction",
                    "--routines",
                    "--triggers",
                    "--events",
                    "--default-character-set=utf8mb4",
                    $"--result-file=\"{sqlPath}\"",
                    $"\"{connection.Database}\""
                });

                var processResult = await RunProcessAsync(
                    mysqldumpPath,
                    args,
                    stdinSqlPath: null,
                    cancellationToken).ConfigureAwait(false);

                details.AppendLine("=== mysqldump stdout ===");
                details.AppendLine(processResult.StandardOutput);
                details.AppendLine("=== mysqldump stderr ===");
                details.AppendLine(processResult.StandardError);

                if (processResult.ExitCode != 0)
                {
                    result.Summary = $"mysqldump failed (exit code {processResult.ExitCode}).";
                    return result;
                }

                if (!File.Exists(sqlPath))
                {
                    result.Summary = "mysqldump finished but no SQL file was produced.";
                    return result;
                }

                details.AppendLine(BackupBrandingAssets(sqlPath, request.ZipCompression));

                progress?.Report("Finalizing backup file...");
                if (request.ZipCompression)
                {
                    var zipPath = Path.ChangeExtension(sqlPath, ".zip");
                    if (File.Exists(zipPath))
                    {
                        File.Delete(zipPath);
                    }

                    using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                    {
                        archive.CreateEntryFromFile(sqlPath, Path.GetFileName(sqlPath), CompressionLevel.Optimal);
                        AddBrandingAssetsToArchive(archive);
                    }

                    File.Delete(sqlPath);
                    backupPath = zipPath;
                }

                var backupInfo = new FileInfo(backupPath);
                if (!ValidateBackupArtifact(backupPath, out var backupValidationMessage))
                {
                    result.Summary = backupValidationMessage;
                    details.AppendLine(backupValidationMessage);
                    return result;
                }

                result.Success = true;
                result.TargetFilePath = backupInfo.FullName;
                result.FileSizeBytes = backupInfo.Exists ? backupInfo.Length : 0;
                result.Summary = "Backup completed successfully.";
                result.Notes = BuildSummary(processResult);
                details.AppendLine(result.Notes);
                progress?.Report(result.Summary);
                return result;
            }
            catch (Exception ex)
            {
                result.Summary = $"Backup error: {ex.Message}";
                details.AppendLine(ex.ToString());
                return result;
            }
            finally
            {
                TryDeleteFile(optionFilePath);

                result.LogFilePath = WriteLog("BACKUP", startedAt, details.ToString());
                if (result.Success && string.IsNullOrWhiteSpace(result.Notes))
                {
                    result.Notes = "Backup completed.";
                }

                if (!string.IsNullOrWhiteSpace(backupPath) && result.FileSizeBytes == 0 && File.Exists(backupPath))
                {
                    result.FileSizeBytes = new FileInfo(backupPath).Length;
                    result.TargetFilePath = backupPath;
                }

                AppendHistory(new BackupRestoreHistoryEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    Action = "Backup",
                    FilePath = result.TargetFilePath,
                    FileSizeBytes = result.FileSizeBytes,
                    Status = result.Success ? "SUCCESS" : "FAILED",
                    Notes = result.Success ? result.Notes : result.Summary,
                    CorrelationId = correlationId
                });

                try
                {
                    AuditTrailService.Log(
                        result.Success ? "BACKUP_SUCCESS" : "BACKUP_FAILED",
                        "database_backup_restore",
                        null,
                        null,
                        new
                        {
                            result.TargetFilePath,
                            result.FileSizeBytes,
                            result.Summary
                        });
                }
                catch
                {
                    // Keep backup/restore result independent from audit logging availability.
                }

                try
                {
                    _operationLogService.Log(
                        PolicyActionKey.MAINTENANCE_BACKUP_EXECUTE,
                        result.Success ? "BACKUP_SUCCESS" : "BACKUP_FAILED",
                        "database_backup_restore",
                        null,
                        result.Success ? GovernedOperationStatus.SUCCEEDED : GovernedOperationStatus.FAILED,
                        result.Success ? "Database backup completed." : result.Summary,
                        payload: new
                        {
                            result.TargetFilePath,
                            result.FileSizeBytes,
                            result.Summary
                        },
                        correlationId: correlationId);
                }
                catch
                {
                    // Keep backup result independent from operation log availability.
                }
            }
        }

        public async Task<BackupRestoreExecutionResult> RestoreAsync(
            RestoreRequest request,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var correlationId = CorrelationContext.Ensure();
            var result = new BackupRestoreExecutionResult
            {
                Action = "RESTORE",
                Success = false,
                Summary = "Restore failed."
            };

            var details = new StringBuilder();
            var startedAt = DateTime.UtcNow;
            string? optionFilePath = null;
            string? extractedTempDir = null;
            string? extractedBrandingDir = null;
            string sourcePath = request?.RestoreFilePath ?? string.Empty;

            try
            {
                _permissionBoundary.EnsureAllowed(PolicyActionKey.MAINTENANCE_RESTORE_EXECUTE);
                _operationLogService.Log(
                    PolicyActionKey.MAINTENANCE_RESTORE_EXECUTE,
                    "RESTORE_START",
                    "database_backup_restore",
                    null,
                    GovernedOperationStatus.STARTED,
                    "Database restore started.",
                    payload: new { request?.RestoreFilePath },
                    correlationId: correlationId);

                if (request == null)
                {
                    result.Summary = "Restore request is required.";
                    return result;
                }

                if (string.IsNullOrWhiteSpace(request.RestoreFilePath) || !File.Exists(request.RestoreFilePath))
                {
                    result.Summary = "Restore file was not found.";
                    return result;
                }

                progress?.Report("Resolving MySQL tools...");
                if (!TryResolveMySqlTools(request.MySqlBinFolder, out var mysqldumpPath, out var mysqlPath, out var resolveMessage))
                {
                    _ = mysqldumpPath;
                    result.Summary = resolveMessage;
                    details.AppendLine(resolveMessage);
                    return result;
                }

                _ = mysqldumpPath;
                details.AppendLine($"Using mysql: {mysqlPath}");

                var sqlPath = request.RestoreFilePath;
                if (string.Equals(Path.GetExtension(sqlPath), ".zip", StringComparison.OrdinalIgnoreCase))
                {
                    progress?.Report("Extracting SQL from ZIP...");
                    extractedTempDir = Path.Combine(Path.GetTempPath(), $"sms_restore_{Guid.NewGuid():N}");
                    Directory.CreateDirectory(extractedTempDir);
                    ZipFile.ExtractToDirectory(sqlPath, extractedTempDir, overwriteFiles: true);
                    sqlPath = Directory.GetFiles(extractedTempDir, "*.sql", SearchOption.AllDirectories)
                        .OrderByDescending(File.GetLastWriteTimeUtc)
                        .FirstOrDefault() ?? string.Empty;
                    extractedBrandingDir = Path.Combine(extractedTempDir, BrandingAssetsFolderName);

                    if (string.IsNullOrWhiteSpace(sqlPath))
                    {
                        result.Summary = "ZIP does not contain a .sql file.";
                        return result;
                    }
                }

                sourcePath = sqlPath;
                var connection = GetConnectionSettings();
                optionFilePath = CreateOptionFile(connection);

                progress?.Report("Preparing target database...");
                var resetSql = BuildResetDatabaseSql(connection.Database);
                var resetArgs = string.Join(" ", new[]
                {
                    $"--defaults-extra-file=\"{optionFilePath}\"",
                    "--default-character-set=utf8mb4",
                    $"--execute=\"{resetSql}\""
                });

                var resetResult = await RunProcessAsync(
                    mysqlPath,
                    resetArgs,
                    stdinSqlPath: null,
                    cancellationToken).ConfigureAwait(false);

                details.AppendLine("=== mysql reset stdout ===");
                details.AppendLine(resetResult.StandardOutput);
                details.AppendLine("=== mysql reset stderr ===");
                details.AppendLine(resetResult.StandardError);

                if (resetResult.ExitCode != 0)
                {
                    result.Summary = BuildFailureSummary("mysql database reset failed", resetResult);
                    return result;
                }

                progress?.Report("Restoring database. Please wait...");
                var args = string.Join(" ", new[]
                {
                    $"--defaults-extra-file=\"{optionFilePath}\"",
                    $"--database=\"{connection.Database}\"",
                    "--default-character-set=utf8mb4"
                });

                var processResult = await RunProcessAsync(
                    mysqlPath,
                    args,
                    stdinSqlPath: sqlPath,
                    cancellationToken).ConfigureAwait(false);

                details.AppendLine("=== mysql stdout ===");
                details.AppendLine(processResult.StandardOutput);
                details.AppendLine("=== mysql stderr ===");
                details.AppendLine(processResult.StandardError);

                if (processResult.ExitCode != 0)
                {
                    result.Summary = BuildFailureSummary("mysql restore failed", processResult);
                    return result;
                }

                progress?.Report("Verifying restored database...");
                var verifyResult = await VerifyRestoreAsync(
                    mysqlPath,
                    optionFilePath!,
                    connection.Database,
                    cancellationToken).ConfigureAwait(false);
                details.AppendLine("=== restore verification ===");
                details.AppendLine(verifyResult.StandardOutput);
                details.AppendLine(verifyResult.StandardError);
                if (!verifyResult.Success)
                {
                    result.Summary = verifyResult.Message;
                    return result;
                }

                RestoreBrandingAssets(request.RestoreFilePath, extractedBrandingDir, details);

                var sourceInfo = new FileInfo(sourcePath);
                result.Success = true;
                result.TargetFilePath = sourceInfo.FullName;
                result.FileSizeBytes = sourceInfo.Exists ? sourceInfo.Length : 0;
                result.Summary = "Restore completed successfully.";
                result.Notes = BuildSummary(processResult);
                details.AppendLine(result.Notes);
                progress?.Report(result.Summary);
                return result;
            }
            catch (Exception ex)
            {
                result.Summary = $"Restore error: {ex.Message}";
                details.AppendLine(ex.ToString());
                return result;
            }
            finally
            {
                TryDeleteFile(optionFilePath);
                TryDeleteDirectory(extractedTempDir);

                result.LogFilePath = WriteLog("RESTORE", startedAt, details.ToString());
                if (result.Success && string.IsNullOrWhiteSpace(result.Notes))
                {
                    result.Notes = "Restore completed.";
                }

                AppendHistory(new BackupRestoreHistoryEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    Action = "Restore",
                    FilePath = result.TargetFilePath,
                    FileSizeBytes = result.FileSizeBytes,
                    Status = result.Success ? "SUCCESS" : "FAILED",
                    Notes = result.Success ? result.Notes : result.Summary,
                    CorrelationId = correlationId
                });

                try
                {
                    AuditTrailService.Log(
                        result.Success ? "RESTORE_SUCCESS" : "RESTORE_FAILED",
                        "database_backup_restore",
                        null,
                        null,
                        new
                        {
                            result.TargetFilePath,
                            result.FileSizeBytes,
                            result.Summary
                        });
                }
                catch
                {
                    // Keep backup/restore result independent from audit logging availability.
                }

                try
                {
                    _operationLogService.Log(
                        PolicyActionKey.MAINTENANCE_RESTORE_EXECUTE,
                        result.Success ? "RESTORE_SUCCESS" : "RESTORE_FAILED",
                        "database_backup_restore",
                        null,
                        result.Success ? GovernedOperationStatus.SUCCEEDED : GovernedOperationStatus.FAILED,
                        result.Success ? "Database restore completed." : result.Summary,
                        payload: new
                        {
                            result.TargetFilePath,
                            result.FileSizeBytes,
                            result.Summary
                        },
                        correlationId: correlationId);
                }
                catch
                {
                    // Keep restore result independent from operation log availability.
                }
            }
        }

        private static async Task<ProcessExecutionResult> RunProcessAsync(
            string fileName,
            string arguments,
            string? stdinSqlPath,
            CancellationToken cancellationToken)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = !string.IsNullOrWhiteSpace(stdinSqlPath),
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi };
            if (!process.Start())
            {
                throw new InvalidOperationException($"Unable to start process '{fileName}'.");
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(stdinSqlPath))
            {
                await using var inputStream = new FileStream(
                    stdinSqlPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    1024 * 64,
                    useAsync: true);

                await inputStream.CopyToAsync(process.StandardInput.BaseStream, cancellationToken).ConfigureAwait(false);
                await process.StandardInput.FlushAsync().ConfigureAwait(false);
                process.StandardInput.Close();
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var stdout = await stdoutTask.ConfigureAwait(false);
            var stderr = await stderrTask.ConfigureAwait(false);
            return new ProcessExecutionResult(process.ExitCode, stdout, stderr);
        }

        private static async Task<RestoreVerificationResult> VerifyRestoreAsync(
            string mysqlPath,
            string optionFilePath,
            string databaseName,
            CancellationToken cancellationToken)
        {
            var escapedDb = databaseName.Replace("'", "''");
            var tableCountSql = $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='{escapedDb}';";
            var requiredTablesSql =
                $"SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='{escapedDb}' AND table_name IN ('users','students','teachers','enrollments');";

            var commonArgs = $"--defaults-extra-file=\"{optionFilePath}\" --batch --skip-column-names --default-character-set=utf8mb4";
            var tableCountResult = await RunProcessAsync(
                mysqlPath,
                $"{commonArgs} --execute=\"{tableCountSql}\"",
                stdinSqlPath: null,
                cancellationToken).ConfigureAwait(false);

            if (tableCountResult.ExitCode != 0)
            {
                return RestoreVerificationResult.Fail(
                    BuildFailureSummary("Post-restore validation query failed", tableCountResult),
                    tableCountResult.StandardOutput,
                    tableCountResult.StandardError);
            }

            if (!TryParseFirstInteger(tableCountResult.StandardOutput, out var tableCount) || tableCount <= 0)
            {
                return RestoreVerificationResult.Fail(
                    "Post-restore validation failed: no tables found in restored database.",
                    tableCountResult.StandardOutput,
                    tableCountResult.StandardError);
            }

            var requiredTablesResult = await RunProcessAsync(
                mysqlPath,
                $"{commonArgs} --execute=\"{requiredTablesSql}\"",
                stdinSqlPath: null,
                cancellationToken).ConfigureAwait(false);

            if (requiredTablesResult.ExitCode != 0)
            {
                return RestoreVerificationResult.Fail(
                    BuildFailureSummary("Required table validation failed", requiredTablesResult),
                    requiredTablesResult.StandardOutput,
                    requiredTablesResult.StandardError);
            }

            if (!TryParseFirstInteger(requiredTablesResult.StandardOutput, out var requiredCount) || requiredCount < 4)
            {
                return RestoreVerificationResult.Fail(
                    "Post-restore validation failed: required core tables are missing.",
                    requiredTablesResult.StandardOutput,
                    requiredTablesResult.StandardError);
            }

            return RestoreVerificationResult.Ok(
                $"Validated restored database: {tableCount} tables, {requiredCount} core tables found.",
                requiredTablesResult.StandardOutput,
                requiredTablesResult.StandardError);
        }

        private static bool ValidateBackupArtifact(string artifactPath, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(artifactPath) || !File.Exists(artifactPath))
            {
                message = "Backup validation failed: output file was not created.";
                return false;
            }

            var info = new FileInfo(artifactPath);
            if (info.Length <= 0)
            {
                message = "Backup validation failed: output file is empty.";
                return false;
            }

            if (string.Equals(info.Extension, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = ZipFile.OpenRead(artifactPath);
                var sqlEntry = archive.Entries.FirstOrDefault(x =>
                    string.Equals(Path.GetExtension(x.FullName), ".sql", StringComparison.OrdinalIgnoreCase));
                if (sqlEntry == null || sqlEntry.Length <= 0)
                {
                    message = "Backup validation failed: ZIP does not contain a valid SQL file.";
                    return false;
                }

                using var stream = sqlEntry.Open();
                var preview = ReadPreviewText(stream);
                if (!LooksLikeSqlDump(preview))
                {
                    message = "Backup validation failed: SQL content appears invalid.";
                    return false;
                }

                return true;
            }

            if (string.Equals(info.Extension, ".sql", StringComparison.OrdinalIgnoreCase))
            {
                using var file = new FileStream(artifactPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var preview = ReadPreviewText(file);
                if (!LooksLikeSqlDump(preview))
                {
                    message = "Backup validation failed: SQL content appears invalid.";
                    return false;
                }

                return true;
            }

            message = "Backup validation failed: unsupported backup artifact type.";
            return false;
        }

        private static string ReadPreviewText(Stream stream)
        {
            const int maxBytes = 1024 * 1024;
            using var ms = new MemoryStream();
            var buffer = new byte[8192];
            var remaining = maxBytes;

            while (remaining > 0)
            {
                var read = stream.Read(buffer, 0, Math.Min(buffer.Length, remaining));
                if (read <= 0)
                {
                    break;
                }

                ms.Write(buffer, 0, read);
                remaining -= read;
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static bool LooksLikeSqlDump(string text)
        {
            var normalized = (text ?? string.Empty).ToUpperInvariant();
            return normalized.Contains("CREATE TABLE") ||
                   normalized.Contains("INSERT INTO") ||
                   normalized.Contains("DROP TABLE") ||
                   normalized.Contains("LOCK TABLES");
        }

        private static HashSet<string> DiscoverSqlTables(string sqlPath)
        {
            var discovered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var stream = new FileStream(sqlPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                TryCaptureTableName(discovered, line, "CREATE TABLE");
                TryCaptureTableName(discovered, line, "INSERT INTO");
                TryCaptureTableName(discovered, line, "DROP TABLE");
            }

            return discovered;
        }

        private static void TryCaptureTableName(HashSet<string> discovered, string line, string marker)
        {
            var normalizedLine = line.Trim();
            if (!normalizedLine.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var tokens = normalizedLine
                .Replace("`", " ")
                .Replace("(", " ")
                .Replace(";", " ")
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            // patterns:
            // CREATE TABLE <name>
            // CREATE TABLE IF NOT EXISTS <name>
            // INSERT INTO <name>
            // DROP TABLE IF EXISTS <name>
            var nameIndex = marker.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase)
                ? ResolveCreateTableNameIndex(tokens)
                : marker.StartsWith("DROP", StringComparison.OrdinalIgnoreCase)
                    ? ResolveDropTableNameIndex(tokens)
                    : ResolveInsertIntoNameIndex(tokens);
            if (nameIndex < 0 || nameIndex >= tokens.Count)
            {
                return;
            }

            var tableName = tokens[nameIndex]
                .Trim()
                .Trim('`')
                .Trim()
                .ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(tableName))
            {
                return;
            }

            discovered.Add(tableName);
        }

        private static int ResolveCreateTableNameIndex(IReadOnlyList<string> tokens)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (!string.Equals(tokens[i], "TABLE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (i + 1 < tokens.Count &&
                    string.Equals(tokens[i + 1], "IF", StringComparison.OrdinalIgnoreCase))
                {
                    return i + 4 < tokens.Count ? i + 4 : -1;
                }

                return i + 1 < tokens.Count ? i + 1 : -1;
            }

            return -1;
        }

        private static int ResolveDropTableNameIndex(IReadOnlyList<string> tokens)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (!string.Equals(tokens[i], "TABLE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (i + 1 < tokens.Count &&
                    string.Equals(tokens[i + 1], "IF", StringComparison.OrdinalIgnoreCase))
                {
                    return i + 3 < tokens.Count ? i + 3 : -1;
                }

                return i + 1 < tokens.Count ? i + 1 : -1;
            }

            return -1;
        }

        private static int ResolveInsertIntoNameIndex(IReadOnlyList<string> tokens)
        {
            for (var i = 0; i < tokens.Count; i++)
            {
                if (string.Equals(tokens[i], "INTO", StringComparison.OrdinalIgnoreCase))
                {
                    return i + 1 < tokens.Count ? i + 1 : -1;
                }
            }

            return -1;
        }

        private static bool TryParseFirstInteger(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var first = text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .FirstOrDefault(x => x.Length > 0);
            return int.TryParse(first, out value);
        }

        private static string BuildSummary(ProcessExecutionResult processResult)
        {
            var stderr = NormalizeText(processResult.StandardError);
            var stdout = NormalizeText(processResult.StandardOutput);

            if (processResult.ExitCode == 0)
            {
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    return Truncate($"Completed with warnings: {stderr}", 300);
                }

                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    return Truncate(stdout, 300);
                }

                return "Completed successfully.";
            }

            var message = !string.IsNullOrWhiteSpace(stderr) ? stderr : stdout;
            if (string.IsNullOrWhiteSpace(message))
            {
                message = $"Process failed with exit code {processResult.ExitCode}.";
            }

            return Truncate(message, 300);
        }

        private static string BuildFailureSummary(string prefix, ProcessExecutionResult processResult)
        {
            var detail = BuildSummary(processResult);
            if (string.IsNullOrWhiteSpace(detail) || detail.StartsWith("Process failed with exit code", StringComparison.OrdinalIgnoreCase))
            {
                return $"{prefix} (exit code {processResult.ExitCode}).";
            }

            return Truncate($"{prefix} (exit code {processResult.ExitCode}): {detail}", 350);
        }

        private static string NormalizeText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var lines = value
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Take(5);

            return string.Join(" | ", lines);
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength).TrimEnd() + "...";
        }

        private static DbConnectionSettings GetConnectionSettings()
        {
            using var db = new AppDbContext();
            var connectionString = db.Database.GetDbConnection().ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string is empty.");
            }

            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            var database = FirstValue(builder, "database", "initial catalog");
            var server = FirstValue(builder, "server", "host", "data source");
            var user = FirstValue(builder, "uid", "user id", "user", "username");
            var password = FirstValue(builder, "pwd", "password");
            var portText = FirstValue(builder, "port");

            if (string.IsNullOrWhiteSpace(database) ||
                string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(user))
            {
                throw new InvalidOperationException("Database connection settings are incomplete.");
            }

            var port = 3306;
            if (!string.IsNullOrWhiteSpace(portText) && int.TryParse(portText, out var parsedPort) && parsedPort > 0)
            {
                port = parsedPort;
            }

            return new DbConnectionSettings
            {
                Database = database,
                Host = server,
                User = user,
                Password = password,
                Port = port
            };
        }

        private static string FirstValue(DbConnectionStringBuilder builder, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (builder.TryGetValue(key, out var value) && value != null)
                {
                    var text = value.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text.Trim();
                    }
                }
            }

            return string.Empty;
        }

        private static string BuildResetDatabaseSql(string databaseName)
        {
            var quoted = QuoteIdentifier(databaseName);
            return string.Join(" ", new[]
            {
                "SET FOREIGN_KEY_CHECKS=0;",
                $"DROP DATABASE IF EXISTS {quoted};",
                $"CREATE DATABASE {quoted} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
                "SET FOREIGN_KEY_CHECKS=1;"
            });
        }

        private static string QuoteIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                throw new InvalidOperationException("Database name is missing from the connection settings.");
            }

            var trimmed = identifier.Trim();
            return $"`{trimmed.Replace("`", "``")}`";
        }

        private static string CreateOptionFile(DbConnectionSettings connection)
        {
            var file = Path.Combine(Path.GetTempPath(), $"sms_mysql_{Guid.NewGuid():N}.cnf");
            var lines = new[]
            {
                "[client]",
                $"host=\"{EscapeOption(connection.Host)}\"",
                $"port={connection.Port}",
                $"user=\"{EscapeOption(connection.User)}\"",
                $"password=\"{EscapeOption(connection.Password)}\"",
                "protocol=tcp"
            };

            File.WriteAllLines(file, lines, new UTF8Encoding(false));
            return file;
        }

        private static string EscapeOption(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }

        private static void TryDeleteFile(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        private static void TryDeleteDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        private bool TryResolveMySqlTools(
            string? preferredBinFolder,
            out string mysqldumpPath,
            out string mysqlPath,
            out string message)
        {
            mysqldumpPath = string.Empty;
            mysqlPath = string.Empty;
            message = string.Empty;

            if (!string.IsNullOrWhiteSpace(preferredBinFolder))
            {
                var preferredDump = Path.Combine(preferredBinFolder, "mysqldump.exe");
                var preferredMySql = Path.Combine(preferredBinFolder, "mysql.exe");
                if (File.Exists(preferredDump) && File.Exists(preferredMySql))
                {
                    mysqldumpPath = preferredDump;
                    mysqlPath = preferredMySql;
                    return true;
                }
            }

            if (TryFindOnPath("mysqldump.exe", out var dumpOnPath) &&
                TryFindOnPath("mysql.exe", out var mysqlOnPath))
            {
                mysqldumpPath = dumpOnPath;
                mysqlPath = mysqlOnPath;
                return true;
            }

            foreach (var binFolder in GetCommonMySqlBinFolders())
            {
                var dump = Path.Combine(binFolder, "mysqldump.exe");
                var mysql = Path.Combine(binFolder, "mysql.exe");
                if (File.Exists(dump) && File.Exists(mysql))
                {
                    mysqldumpPath = dump;
                    mysqlPath = mysql;
                    return true;
                }
            }

            message = "MySQL tools were not found. Install MySQL client tools or choose the MySQL bin folder.";
            return false;
        }

        private static bool TryFindOnPath(string executableName, out string executablePath)
        {
            executablePath = string.Empty;
            var pathValue = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathValue))
            {
                return false;
            }

            var paths = pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var path in paths)
            {
                try
                {
                    var candidate = Path.Combine(path, executableName);
                    if (File.Exists(candidate))
                    {
                        executablePath = candidate;
                        return true;
                    }
                }
                catch
                {
                    // Ignore bad PATH entries.
                }
            }

            return false;
        }

        private static IEnumerable<string> GetCommonMySqlBinFolders()
        {
            var list = new List<string>();

            static void AddIfExists(List<string> target, string? folderPath)
            {
                if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
                {
                    target.Add(folderPath);
                }
            }

            AddIfExists(list, @"C:\xampp\mysql\bin");

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            AddMySqlBinsUnderRoot(list, Path.Combine(programFiles, "MySQL"));
            AddMySqlBinsUnderRoot(list, Path.Combine(programFilesX86, "MySQL"));
            AddMySqlBinsUnderVersionedRoot(list, @"C:\wamp64\bin\mysql");
            AddMySqlBinsUnderVersionedRoot(list, @"C:\laragon\bin\mysql");

            return list.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static void AddMySqlBinsUnderRoot(List<string> list, string root)
        {
            if (!Directory.Exists(root))
            {
                return;
            }

            try
            {
                foreach (var dir in Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly))
                {
                    var bin = Path.Combine(dir, "bin");
                    if (Directory.Exists(bin))
                    {
                        list.Add(bin);
                    }
                }
            }
            catch
            {
                // Ignore folder enumeration errors.
            }
        }

        private static void AddMySqlBinsUnderVersionedRoot(List<string> list, string root)
        {
            if (!Directory.Exists(root))
            {
                return;
            }

            try
            {
                foreach (var versionDir in Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly))
                {
                    var bin = Path.Combine(versionDir, "bin");
                    if (Directory.Exists(bin))
                    {
                        list.Add(bin);
                    }
                }
            }
            catch
            {
                // Ignore folder enumeration errors.
            }
        }

        private List<BackupRestoreHistoryEntry> ReadHistoryCore()
        {
            var path = GetHistoryFilePath();
            if (!File.Exists(path))
            {
                return new List<BackupRestoreHistoryEntry>();
            }

            try
            {
                var json = File.ReadAllText(path);
                var history = JsonSerializer.Deserialize<List<BackupRestoreHistoryEntry>>(json, JsonOptions);
                return history ?? new List<BackupRestoreHistoryEntry>();
            }
            catch
            {
                return new List<BackupRestoreHistoryEntry>();
            }
        }

        private void AppendHistory(BackupRestoreHistoryEntry entry)
        {
            try
            {
                var history = ReadHistoryCore();
                history.Add(entry);
                history = history
                    .OrderByDescending(x => x.TimestampUtc)
                    .Take(MaxHistoryEntries)
                    .ToList();

                var path = GetHistoryFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(history, JsonOptions), Encoding.UTF8);
            }
            catch
            {
                // History persistence should not fail the feature workflow.
            }
        }

        private string WriteLog(string action, DateTime startedAtUtc, string detail)
        {
            var logsDir = GetLogsDirectory();
            Directory.CreateDirectory(logsDir);
            var filePath = Path.Combine(logsDir, $"{DateTime.Now:yyyyMMdd}.log");

            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {action}");
            sb.AppendLine($"StartedUtc: {startedAtUtc:O}");
            if (!string.IsNullOrWhiteSpace(detail))
            {
                sb.AppendLine(detail.TrimEnd());
            }

            sb.AppendLine(new string('-', 80));
            File.AppendAllText(filePath, sb.ToString(), Encoding.UTF8);
            return filePath;
        }

        private static string GetDefaultBackupFolder()
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, ResolveAppName(), "Backups");
        }

        private static string ResolveAppName()
        {
            var productName = Application.ProductName;
            if (string.IsNullOrWhiteSpace(productName))
            {
                productName = "School Management System";
            }

            return productName.Trim();
        }

        private static string GetAppDataDirectory()
        {
            var appData = GetApplicationDataRoot();
            var root = Path.Combine(appData, ResolveAppName(), "BackupRestore");
            Directory.CreateDirectory(root);
            return root;
        }

        private static string GetLogsDirectory()
        {
            var appData = GetApplicationDataRoot();
            var logs = Path.Combine(appData, ResolveAppName(), "Logs", "BackupRestore");
            Directory.CreateDirectory(logs);
            return logs;
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

        private static string GetSettingsFilePath()
        {
            return Path.Combine(GetAppDataDirectory(), "settings.json");
        }

        private static string GetHistoryFilePath()
        {
            return Path.Combine(GetAppDataDirectory(), "history.json");
        }

        private static string BackupBrandingAssets(string sqlPath, bool zipCompression)
        {
            var sourceDir = FileStorageService.GetBrandingStorageDirectory();
            if (!Directory.Exists(sourceDir))
            {
                return "Branding assets: no custom logo files found.";
            }

            var files = Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
            {
                return "Branding assets: no custom logo files found.";
            }

            if (zipCompression)
            {
                return $"Branding assets: {files.Length} file(s) queued for ZIP packaging.";
            }

            var targetDir = GetCompanionBrandingAssetsDirectory(sqlPath);
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, recursive: true);
            }

            Directory.CreateDirectory(targetDir);
            foreach (var file in files)
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            }

            return $"Branding assets: copied {files.Length} file(s) to companion folder '{targetDir}'.";
        }

        private static void AddBrandingAssetsToArchive(ZipArchive archive)
        {
            var sourceDir = FileStorageService.GetBrandingStorageDirectory();
            if (!Directory.Exists(sourceDir))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                archive.CreateEntryFromFile(
                    file,
                    $"{BrandingAssetsFolderName}/{Path.GetFileName(file)}",
                    CompressionLevel.Optimal);
            }
        }

        private static void RestoreBrandingAssets(string restoreFilePath, string? extractedBrandingDir, StringBuilder details)
        {
            var sourceDir = ResolveRestoreBrandingAssetsDirectory(restoreFilePath, extractedBrandingDir);
            var targetDir = FileStorageService.GetBrandingStorageDirectory();
            Directory.CreateDirectory(targetDir);

            if (string.IsNullOrWhiteSpace(sourceDir) || !Directory.Exists(sourceDir))
            {
                details.AppendLine("Branding assets: none found in restore artifact. Default logo fallback remains available.");
                return;
            }

            var assetFiles = Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly);
            foreach (var existing in Directory.GetFiles(targetDir, "*", SearchOption.TopDirectoryOnly))
            {
                File.Delete(existing);
            }

            foreach (var file in assetFiles)
            {
                File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)), true);
            }

            details.AppendLine($"Branding assets: restored {assetFiles.Length} file(s).");
        }

        private static string? ResolveRestoreBrandingAssetsDirectory(string restoreFilePath, string? extractedBrandingDir)
        {
            if (!string.IsNullOrWhiteSpace(extractedBrandingDir) && Directory.Exists(extractedBrandingDir))
            {
                return extractedBrandingDir;
            }

            if (!string.Equals(Path.GetExtension(restoreFilePath), ".sql", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var companionDir = GetCompanionBrandingAssetsDirectory(restoreFilePath);
            return Directory.Exists(companionDir) ? companionDir : null;
        }

        private static string GetCompanionBrandingAssetsDirectory(string sqlPath)
        {
            var folder = Path.GetDirectoryName(sqlPath) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(sqlPath);
            return Path.Combine(folder, $"{fileName}.{BrandingAssetsFolderName}");
        }

        private sealed class DbConnectionSettings
        {
            public string Host { get; set; } = string.Empty;
            public int Port { get; set; } = 3306;
            public string User { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Database { get; set; } = string.Empty;
        }

        private sealed class ProcessExecutionResult
        {
            public ProcessExecutionResult(int exitCode, string standardOutput, string standardError)
            {
                ExitCode = exitCode;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            public int ExitCode { get; }
            public string StandardOutput { get; }
            public string StandardError { get; }
        }

        private sealed class RestoreVerificationResult
        {
            private RestoreVerificationResult(bool success, string message, string standardOutput, string standardError)
            {
                Success = success;
                Message = message;
                StandardOutput = standardOutput;
                StandardError = standardError;
            }

            public bool Success { get; }
            public string Message { get; }
            public string StandardOutput { get; }
            public string StandardError { get; }

            public static RestoreVerificationResult Ok(string message, string standardOutput, string standardError)
            {
                return new RestoreVerificationResult(true, message, standardOutput, standardError);
            }

            public static RestoreVerificationResult Fail(string message, string standardOutput, string standardError)
            {
                return new RestoreVerificationResult(false, message, standardOutput, standardError);
            }
        }
    }

    internal sealed class BackupRestoreSettings
    {
        public string BackupFolder { get; set; } = string.Empty;
        public bool ZipCompression { get; set; }
        public string MySqlBinFolder { get; set; } = string.Empty;
    }

    internal sealed class BackupRequest
    {
        public string BackupFolder { get; set; } = string.Empty;
        public bool ZipCompression { get; set; }
        public string MySqlBinFolder { get; set; } = string.Empty;
    }

    internal sealed class RestoreRequest
    {
        public string RestoreFilePath { get; set; } = string.Empty;
        public string MySqlBinFolder { get; set; } = string.Empty;
    }

    internal sealed class BackupRestoreExecutionResult
    {
        public string Action { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string TargetFilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string LogFilePath { get; set; } = string.Empty;
    }

    internal sealed class BackupRestoreHistoryEntry
    {
        public DateTime TimestampUtc { get; set; }
        public string Action { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
    }

    internal sealed class RestorePreflightResult
    {
        public bool Success { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<string> BlockingReasons { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<PreflightCheckResult> Checks { get; set; } = new();

        public static RestorePreflightResult Ok(string summary, IEnumerable<string>? warnings = null, IEnumerable<PreflightCheckResult>? checks = null)
        {
            return new RestorePreflightResult
            {
                Success = true,
                Summary = summary,
                Warnings = warnings?.ToList() ?? new List<string>(),
                Checks = checks?.ToList() ?? new List<PreflightCheckResult>()
            };
        }

        public static RestorePreflightResult Fail(
            string summary,
            IEnumerable<string>? blockers = null,
            IEnumerable<string>? warnings = null,
            IEnumerable<PreflightCheckResult>? checks = null)
        {
            return new RestorePreflightResult
            {
                Success = false,
                Summary = summary,
                BlockingReasons = blockers?.ToList() ?? new List<string>(),
                Warnings = warnings?.ToList() ?? new List<string>(),
                Checks = checks?.ToList() ?? new List<PreflightCheckResult>()
            };
        }
    }
}

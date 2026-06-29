using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;

namespace School_Management_System.Services
{
    internal class ArchiveRecordService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly PermissionBoundaryService _permissionBoundary = new();
        private readonly GovernedOperationLogService _operationLogService = new();
        private readonly ExceptionQueueService _exceptionQueueService = new();

        public IEnumerable<ArchiveRecord> GetAll()
        {
            using var db = new AppDbContext();
            var repo = new ArchiveRecordRepository(db);
            return repo.GetAll();
        }

        public ArchiveRecord? GetById(long id)
        {
            using var db = new AppDbContext();
            var repo = new ArchiveRecordRepository(db);
            return repo.GetById(id);
        }

        public void Create(ArchiveRecord entity)
        {
            using var db = new AppDbContext();
            var repo = new ArchiveRecordRepository(db);
            repo.Add(entity);
        }

        public void Update(ArchiveRecord entity)
        {
            using var db = new AppDbContext();
            var repo = new ArchiveRecordRepository(db);
            repo.Update(entity);
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var repo = new ArchiveRecordRepository(db);
            repo.Delete(id);
        }

        public OperationResult<ArchiveRestoreImpactPreview> BuildRestoreImpactPreview(long archiveRecordId)
        {
            using var db = new AppDbContext();
            var record = db.ArchiveRecords.FirstOrDefault(x => x.Id == archiveRecordId);
            if (record == null)
            {
                return OperationResult<ArchiveRestoreImpactPreview>.Fail("Archive record not found.");
            }

            var preview = new ArchiveRestoreImpactPreview
            {
                ArchiveRecordId = record.Id,
                EntityType = record.EntityType ?? string.Empty,
                OriginalEntityId = record.OriginalEntityId
            };

            if (record.IsRestored)
            {
                preview.BlockingReasons.Add("Archive record was already restored.");
            }

            var targetType = ResolveModelType(record.EntityType ?? string.Empty);
            if (targetType == null)
            {
                preview.BlockingReasons.Add($"Restore is not supported for entity type '{record.EntityType}'.");
                return OperationResult<ArchiveRestoreImpactPreview>.Ok(preview, "Restore impact preview generated.");
            }

            preview.Dependencies.AddRange(BuildDependencyImpact(db, targetType, record.OriginalEntityId));

            var strategy = "PAYLOAD_RECREATE";
            object? candidate = null;
            if (IsSoftDeleteRecord(record) && record.OriginalEntityId.HasValue)
            {
                var existing = db.Find(targetType, record.OriginalEntityId.Value);
                if (existing != null)
                {
                    strategy = "SOFT_DELETE_REACTIVATION";
                    candidate = existing;
                }
                else
                {
                    preview.Warnings.Add("Original soft-deleted entity was not found. Restore will use payload recreation.");
                }
            }

            if (candidate == null)
            {
                var payloadResult = TryDeserializePayload(record.Payload, targetType);
                if (!payloadResult.Success || payloadResult.Data == null)
                {
                    preview.BlockingReasons.Add(payloadResult.Message);
                    preview.RestoreStrategy = strategy;
                    return OperationResult<ArchiveRestoreImpactPreview>.Ok(preview, "Restore impact preview generated.");
                }

                candidate = payloadResult.Data;
            }

            preview.RestoreStrategy = strategy;
            var payloadMode = string.Equals(strategy, "PAYLOAD_RECREATE", StringComparison.OrdinalIgnoreCase);
            preview.BlockingReasons.AddRange(BuildRestoreBlockingReasons(db, candidate, payloadMode));
            return OperationResult<ArchiveRestoreImpactPreview>.Ok(preview, "Restore impact preview generated.");
        }

        public OperationResult<bool> Restore(long archiveRecordId)
        {
            using var correlationScope = CorrelationContext.BeginScope();
            var correlationId = CorrelationContext.Ensure();
            try
            {
                _permissionBoundary.EnsureAllowed(PolicyActionKey.ARCHIVE_RESTORE);
            }
            catch (DomainValidationException ex)
            {
                return OperationResult<bool>.Fail(ex.Message, ex.Details);
            }

            _operationLogService.Log(
                PolicyActionKey.ARCHIVE_RESTORE,
                "ARCHIVE_RESTORE_START",
                "archive_records",
                archiveRecordId,
                GovernedOperationStatus.STARTED,
                "Archive restore started.",
                correlationId: correlationId);

            var previewResult = BuildRestoreImpactPreview(archiveRecordId);
            if (!previewResult.Success || previewResult.Data == null)
            {
                _operationLogService.Log(
                    PolicyActionKey.ARCHIVE_RESTORE,
                    "ARCHIVE_RESTORE_BLOCKED",
                    "archive_records",
                    archiveRecordId,
                    GovernedOperationStatus.BLOCKED,
                    previewResult.Message,
                    payload: previewResult.Errors,
                    correlationId: correlationId);
                return OperationResult<bool>.Fail(previewResult.Message, previewResult.Errors);
            }

            if (!previewResult.Data.CanProceed)
            {
                _operationLogService.Log(
                    PolicyActionKey.ARCHIVE_RESTORE,
                    "ARCHIVE_RESTORE_BLOCKED",
                    "archive_records",
                    archiveRecordId,
                    GovernedOperationStatus.BLOCKED,
                    "Restore preflight failed. Resolve blocking reasons before retrying.",
                    payload: previewResult.Data.BlockingReasons,
                    correlationId: correlationId);

                _exceptionQueueService.Raise(new ExceptionQueueCreateRequest
                {
                    Category = "DEPENDENCY_BLOCK",
                    SourceModule = "Archive.Restore",
                    Entity = "archive_records",
                    EntityId = archiveRecordId,
                    Severity = ExceptionQueueSeverity.WARNING,
                    Summary = "Archive restore blocked by dependency checks.",
                    Details = string.Join(Environment.NewLine, previewResult.Data.BlockingReasons),
                    CorrelationId = correlationId
                });

                return OperationResult<bool>.Fail(
                    "Restore preflight failed. Resolve blocking reasons before retrying.",
                    previewResult.Data.BlockingReasons);
            }

            using var db = new AppDbContext();
            var record = db.ArchiveRecords.FirstOrDefault(x => x.Id == archiveRecordId);
            if (record == null)
            {
                _operationLogService.Log(
                    PolicyActionKey.ARCHIVE_RESTORE,
                    "ARCHIVE_RESTORE_BLOCKED",
                    "archive_records",
                    archiveRecordId,
                    GovernedOperationStatus.BLOCKED,
                    "Archive record not found.",
                    correlationId: correlationId);
                return OperationResult<bool>.Fail("Archive record not found.");
            }

            var targetType = ResolveModelType(record.EntityType ?? string.Empty);
            if (targetType == null)
            {
                _operationLogService.Log(
                    PolicyActionKey.ARCHIVE_RESTORE,
                    "ARCHIVE_RESTORE_BLOCKED",
                    "archive_records",
                    archiveRecordId,
                    GovernedOperationStatus.BLOCKED,
                    $"Restore is not supported for entity type '{record.EntityType}'.",
                    correlationId: correlationId);
                return OperationResult<bool>.Fail($"Restore is not supported for entity type '{record.EntityType}'.");
            }

            try
            {
                if (IsSoftDeleteRecord(record) && record.OriginalEntityId.HasValue)
                {
                    var restored = TryReactivateSoftDeletedEntity(db, targetType, record.OriginalEntityId.Value);
                    if (restored)
                    {
                        MarkArchiveRecordRestored(db, record, "SOFT_DELETE_REACTIVATION");
                        _operationLogService.Log(
                            PolicyActionKey.ARCHIVE_RESTORE,
                            "ARCHIVE_RESTORE_SUCCESS",
                            "archive_records",
                            archiveRecordId,
                            GovernedOperationStatus.SUCCEEDED,
                            "Record reactivated successfully.",
                            correlationId: correlationId);
                        return OperationResult<bool>.Ok(true, "Record reactivated successfully.");
                    }
                }

                var model = JsonSerializer.Deserialize(record.Payload, targetType, JsonOptions);
                if (model == null)
                {
                    _operationLogService.Log(
                        PolicyActionKey.ARCHIVE_RESTORE,
                        "ARCHIVE_RESTORE_FAILED",
                        "archive_records",
                        archiveRecordId,
                        GovernedOperationStatus.FAILED,
                        "Archive payload is empty or invalid.",
                        correlationId: correlationId);
                    return OperationResult<bool>.Fail("Archive payload is empty or invalid.");
                }

                var idProp = targetType.GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
                idProp?.SetValue(model, Convert.ChangeType(0L, idProp.PropertyType));
                SetDateIfExists(model, "CreatedAt", DateTime.UtcNow);
                SetDateIfExists(model, "UpdatedAt", DateTime.UtcNow);

                db.Add(model);
                db.SaveChanges();

                MarkArchiveRecordRestored(db, record, "PAYLOAD_RECREATE");
                _operationLogService.Log(
                    PolicyActionKey.ARCHIVE_RESTORE,
                    "ARCHIVE_RESTORE_SUCCESS",
                    "archive_records",
                    archiveRecordId,
                    GovernedOperationStatus.SUCCEEDED,
                    "Record restored successfully.",
                    correlationId: correlationId);
                return OperationResult<bool>.Ok(true, "Record restored successfully.");
            }
            catch (Exception ex)
            {
                _operationLogService.Log(
                    PolicyActionKey.ARCHIVE_RESTORE,
                    "ARCHIVE_RESTORE_FAILED",
                    "archive_records",
                    archiveRecordId,
                    GovernedOperationStatus.FAILED,
                    ex.Message,
                    correlationId: correlationId);
                return OperationResult<bool>.Fail($"Restore failed: {ex.Message}");
            }
        }

        private static OperationResult<object> TryDeserializePayload(string payload, Type targetType)
        {
            try
            {
                var model = JsonSerializer.Deserialize(payload ?? string.Empty, targetType, JsonOptions);
                return model == null
                    ? OperationResult<object>.Fail("Archive payload is empty or invalid.")
                    : OperationResult<object>.Ok(model);
            }
            catch (Exception ex)
            {
                return OperationResult<object>.Fail($"Archive payload is invalid: {ex.Message}");
            }
        }

        private static List<ArchiveDependencyImpactItem> BuildDependencyImpact(AppDbContext db, Type targetType, long? originalEntityId)
        {
            var items = new List<ArchiveDependencyImpactItem>();
            if (!originalEntityId.HasValue)
            {
                return items;
            }

            foreach (var entityType in db.Model.GetEntityTypes())
            {
                var dependentType = entityType.ClrType;
                if (dependentType == null)
                {
                    continue;
                }

                foreach (var fk in entityType.GetForeignKeys().Where(x => x.PrincipalEntityType.ClrType == targetType))
                {
                    if (fk.Properties.Count != 1)
                    {
                        continue;
                    }

                    var foreignKeyName = fk.Properties[0].Name;
                    var foreignKeyProperty = dependentType.GetProperty(foreignKeyName, BindingFlags.Public | BindingFlags.Instance);
                    if (foreignKeyProperty == null || !foreignKeyProperty.CanRead)
                    {
                        continue;
                    }

                    var count = CountLinkedRows(db, dependentType, foreignKeyProperty, originalEntityId.Value);
                    if (count <= 0)
                    {
                        continue;
                    }

                    items.Add(new ArchiveDependencyImpactItem(
                        dependentEntity: dependentType.Name,
                        relation: $"{dependentType.Name}.{foreignKeyName}",
                        count: count));
                }
            }

            return items
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.DependentEntity, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static int CountLinkedRows(AppDbContext db, Type dependentType, PropertyInfo foreignKeyProperty, long originalEntityId)
        {
            // Use EF's generic DbSet to run a server-side COUNT instead of loading all rows into memory
            var dbSetProperty = typeof(AppDbContext)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p =>
                    p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                    p.PropertyType.GenericTypeArguments[0] == dependentType);

            if (dbSetProperty == null)
            {
                return 0;
            }

            // Build a LINQ expression: dbSet.Count(e => (long)e.ForeignKeyProp == originalEntityId)
            try
            {
                var dbSet = dbSetProperty.GetValue(db);
                if (dbSet == null)
                {
                    return 0;
                }

                // entity parameter
                var param = System.Linq.Expressions.Expression.Parameter(dependentType, "e");
                var propAccess = System.Linq.Expressions.Expression.Property(param, foreignKeyProperty);

                // Handle nullable FK (long?) by unwrapping
                System.Linq.Expressions.Expression comparison;
                if (foreignKeyProperty.PropertyType == typeof(long?))
                {
                    var hasValue = System.Linq.Expressions.Expression.Property(propAccess, "HasValue");
                    var value = System.Linq.Expressions.Expression.Property(propAccess, "Value");
                    var equals = System.Linq.Expressions.Expression.Equal(value,
                        System.Linq.Expressions.Expression.Constant(originalEntityId, typeof(long)));
                    comparison = System.Linq.Expressions.Expression.AndAlso(hasValue, equals);
                }
                else if (foreignKeyProperty.PropertyType == typeof(long))
                {
                    comparison = System.Linq.Expressions.Expression.Equal(propAccess,
                        System.Linq.Expressions.Expression.Constant(originalEntityId, typeof(long)));
                }
                else
                {
                    // Unsupported FK type — fall back to 0
                    return 0;
                }

                var lambda = System.Linq.Expressions.Expression.Lambda(comparison, param);

                // Call Queryable.Count(IQueryable<T>, Expression<Func<T, bool>>)
                var queryableType = typeof(System.Linq.Queryable);
                var countMethod = queryableType
                    .GetMethods()
                    .First(m => m.Name == "Count" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(dependentType);

                var queryable = typeof(System.Linq.Queryable)
                    .GetMethods()
                    .First(m => m.Name == "AsQueryable" && m.GetParameters().Length == 1)
                    .MakeGenericMethod(dependentType)
                    .Invoke(null, new[] { dbSet });

                return (int)(countMethod.Invoke(null, new[] { queryable, lambda }) ?? 0);
            }
            catch
            {
                return 0;
            }
        }

        private static List<string> BuildRestoreBlockingReasons(AppDbContext db, object candidate, bool payloadMode)
        {
            var reasons = new List<string>();

            switch (candidate)
            {
                case Student student:
                    if (!db.Users.Any(x => x.Id == student.UserId))
                    {
                        reasons.Add("Linked user account for student no longer exists.");
                    }
                    if (payloadMode && db.Students.Any(x => x.StudentNumber != null && x.StudentNumber.ToLower() == Normalize(student.StudentNumber).ToLower()))
                    {
                        reasons.Add($"Student number conflict: '{student.StudentNumber}'.");
                    }
                    if (payloadMode && db.Students.Any(x => x.Lrn != null && x.Lrn.ToLower() == Normalize(student.Lrn).ToLower()))
                    {
                        reasons.Add($"LRN conflict: '{student.Lrn}'.");
                    }
                    break;

                case Teacher teacher:
                    if (!db.Users.Any(x => x.Id == teacher.UserId))
                    {
                        reasons.Add("Linked user account for teacher no longer exists.");
                    }
                    if (payloadMode &&
                        !string.IsNullOrWhiteSpace(teacher.EmployeeNo) &&
                        db.Teachers.Any(x => x.EmployeeNo != null && x.EmployeeNo.ToLower() == Normalize(teacher.EmployeeNo).ToLower()))
                    {
                        reasons.Add($"Employee number conflict: '{teacher.EmployeeNo}'.");
                    }
                    break;

                case User user:
                    if (payloadMode && db.Users.Any(x => x.Username != null && x.Username.ToLower() == Normalize(user.Username).ToLower()))
                    {
                        reasons.Add($"Username conflict: '{user.Username}'.");
                    }
                    break;

                case Section section:
                    var schoolYear = db.SchoolYears.Find(section.SchoolYearId);
                    if (schoolYear == null)
                    {
                        reasons.Add($"Section school year reference is missing (SchoolYearId: {section.SchoolYearId}).");
                    }
                    else if (schoolYear.IsArchived)
                    {
                        reasons.Add($"Section cannot be restored while school year '{schoolYear.Name}' is archived.");
                    }

                    if (!db.GradeLevels.Any(x => x.Id == section.GradeLevelId))
                    {
                        reasons.Add($"Section grade level reference is missing (GradeLevelId: {section.GradeLevelId}).");
                    }

                    if (payloadMode)
                    {
                        var normalizedSectionName = Normalize(section.Name).ToLower();
                        if (db.Sections.Any(x =>
                            !x.IsArchived &&
                            x.SchoolYearId == section.SchoolYearId &&
                            x.GradeLevelId == section.GradeLevelId &&
                            x.Name != null && x.Name.ToLower() == normalizedSectionName))
                        {
                            reasons.Add($"Section name conflict in target school year/grade: '{section.Name}'.");
                        }
                    }
                    break;

                case Enrollment enrollment:
                    if (!db.SchoolYears.Any(x => x.Id == enrollment.SchoolYearId && !x.IsArchived))
                    {
                        reasons.Add($"Enrollment school year is missing or archived (SchoolYearId: {enrollment.SchoolYearId}).");
                    }
                    if (!db.Sections.Any(x => x.Id == enrollment.SectionId && !x.IsArchived))
                    {
                        reasons.Add($"Enrollment section is missing or archived (SectionId: {enrollment.SectionId}).");
                    }
                    if (!db.Students.Any(x => x.Id == enrollment.StudentId && x.Status == UserStatus.ACTIVE))
                    {
                        reasons.Add($"Enrollment student is missing or inactive (StudentId: {enrollment.StudentId}).");
                    }
                    if (!db.Curricula.Any(x => x.Id == enrollment.CurriculumId))
                    {
                        reasons.Add($"Enrollment curriculum reference is missing (CurriculumId: {enrollment.CurriculumId}).");
                    }
                    if (payloadMode && db.Enrollments.Any(x => x.SchoolYearId == enrollment.SchoolYearId && x.StudentId == enrollment.StudentId))
                    {
                        reasons.Add("Enrollment conflict: same student already has an enrollment in the same school year.");
                    }
                    break;

                case ClassOffering offering:
                    if (!db.SchoolYears.Any(x => x.Id == offering.SchoolYearId && !x.IsArchived))
                    {
                        reasons.Add($"Class offering school year is missing or archived (SchoolYearId: {offering.SchoolYearId}).");
                    }
                    if (!db.Sections.Any(x => x.Id == offering.SectionId && !x.IsArchived && x.SchoolYearId == offering.SchoolYearId))
                    {
                        reasons.Add($"Class offering section is missing, archived, or mismatched (SectionId: {offering.SectionId}).");
                    }
                    if (!db.Subjects.Any(x => x.Id == offering.SubjectId))
                    {
                        reasons.Add($"Class offering subject reference is missing (SubjectId: {offering.SubjectId}).");
                    }
                    if (payloadMode && db.ClassOfferings.Any(x =>
                        x.SchoolYearId == offering.SchoolYearId &&
                        x.SectionId == offering.SectionId &&
                        x.SubjectId == offering.SubjectId))
                    {
                        reasons.Add("Class offering conflict: duplicate school year + section + subject combination.");
                    }
                    break;
            }

            return reasons;
        }

        private static string Normalize(string? value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static void MarkArchiveRecordRestored(AppDbContext db, ArchiveRecord record, string strategy)
        {
            record.IsRestored = true;
            record.RestoredAt = DateTime.UtcNow;
            record.RestoredByUserId = SessionContext.CurrentUser?.Id;
            db.ArchiveRecords.Update(record);
            db.SaveChanges();

            AuditTrailService.Log("RESTORE", "archive_records", record.Id, null, new
            {
                record.EntityType,
                record.OriginalEntityId,
                Strategy = strategy
            });
        }

        private static bool IsSoftDeleteRecord(ArchiveRecord record)
        {
            return (record.Notes ?? string.Empty)
                .Contains("SOFT_DELETE", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryReactivateSoftDeletedEntity(AppDbContext db, Type targetType, long originalEntityId)
        {
            var entity = db.Find(targetType, originalEntityId);
            if (entity == null)
            {
                return false;
            }

            var statusProp = targetType.GetProperty("Status", BindingFlags.Public | BindingFlags.Instance);
            if (statusProp != null && statusProp.CanWrite && statusProp.PropertyType.IsEnum)
            {
                var activeName = Enum.GetNames(statusProp.PropertyType)
                    .FirstOrDefault(n => string.Equals(n, "ACTIVE", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(activeName))
                {
                    var activeValue = Enum.Parse(statusProp.PropertyType, activeName);
                    statusProp.SetValue(entity, activeValue);
                }
            }

            var isActiveProp = targetType.GetProperty("IsActive", BindingFlags.Public | BindingFlags.Instance);
            if (isActiveProp != null && isActiveProp.CanWrite && isActiveProp.PropertyType == typeof(bool))
            {
                isActiveProp.SetValue(entity, true);
            }

            var isArchivedProp = targetType.GetProperty("IsArchived", BindingFlags.Public | BindingFlags.Instance);
            if (isArchivedProp != null && isArchivedProp.CanWrite && isArchivedProp.PropertyType == typeof(bool))
            {
                isArchivedProp.SetValue(entity, false);
            }

            SetDateIfExists(entity, "UpdatedAt", DateTime.UtcNow);
            db.Update(entity);
            db.SaveChanges();
            return true;
        }

        private static Type? ResolveModelType(string entityType)
        {
            if (string.IsNullOrWhiteSpace(entityType))
            {
                return null;
            }

            var normalized = entityType.Trim();
            var assembly = typeof(User).Assembly;
            return assembly.GetTypes()
                .FirstOrDefault(t =>
                    t.Namespace == typeof(User).Namespace &&
                    string.Equals(t.Name, normalized, StringComparison.OrdinalIgnoreCase) &&
                    typeof(IBaseModel).IsAssignableFrom(t));
        }

        private static void SetDateIfExists(object instance, string propertyName, DateTime value)
        {
            var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null || !prop.CanWrite)
            {
                return;
            }

            if (prop.PropertyType == typeof(DateTime))
            {
                prop.SetValue(instance, value);
            }
            else if (prop.PropertyType == typeof(DateTime?))
            {
                prop.SetValue(instance, value);
            }
        }
    }
}

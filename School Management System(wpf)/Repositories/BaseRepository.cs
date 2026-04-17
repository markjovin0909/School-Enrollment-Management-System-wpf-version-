using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Repositories
{
    internal class BaseRepository<T> : IBaseRepository<T>
        where T : class, IBaseModel
    {
        protected readonly Data.AppDbContext _context;

        public BaseRepository(Data.AppDbContext context)
        {
            _context = context;
        }

        public IEnumerable<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }

        public T? GetById(long id)
        {
            return _context.Set<T>().Find(id);
        }

        public void Add(T entity)
        {
            _context.Set<T>().Add(entity);
            _context.SaveChanges();
        }

        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
            _context.SaveChanges();
        }

        public void Delete(long id)
        {
            var entity = GetById(id);
            if (entity != null)
            {
                var now = DateTime.UtcNow;
                var archived = false;
                var softDeleted = false;

                if (typeof(T) != typeof(ArchiveRecord))
                {
                    try
                    {
                        var payload = JsonSerializer.Serialize(entity, entity.GetType());
                        var softDeleteApplied = TryApplySoftDelete(entity, now);
                        var note = softDeleteApplied ? "SOFT_DELETE" : "HARD_DELETE";

                        var archive = new ArchiveRecord
                        {
                            EntityType = typeof(T).Name,
                            OriginalEntityId = entity.Id,
                            Payload = payload,
                            DeletedByUserId = SessionContext.CurrentUser?.Id,
                            DeletedAt = now,
                            IsRestored = false,
                            Notes = note
                        };
                        _context.Set<ArchiveRecord>().Add(archive);
                        archived = true;
                        softDeleted = softDeleteApplied;
                    }
                    catch
                    {
                        // Archive capture is best-effort and should not block delete operations.
                    }
                }

                if (!archived)
                {
                    softDeleted = TryApplySoftDelete(entity, now);
                }

                if (softDeleted)
                {
                    _context.Set<T>().Update(entity);
                }
                else
                {
                    _context.Set<T>().Remove(entity);
                }

                _context.SaveChanges();
            }
        }

        private static bool TryApplySoftDelete(object entity, DateTime timestampUtc)
        {
            var type = entity.GetType();

            var statusProp = type.GetProperty("Status", BindingFlags.Public | BindingFlags.Instance);
            if (statusProp != null && statusProp.CanWrite && statusProp.PropertyType.IsEnum)
            {
                var inactiveName = Enum.GetNames(statusProp.PropertyType)
                    .FirstOrDefault(n => string.Equals(n, "INACTIVE", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(inactiveName))
                {
                    var inactiveValue = Enum.Parse(statusProp.PropertyType, inactiveName);
                    statusProp.SetValue(entity, inactiveValue);
                    SetTimestamp(type, entity, timestampUtc);
                    return true;
                }
            }

            var isActiveProp = type.GetProperty("IsActive", BindingFlags.Public | BindingFlags.Instance);
            if (isActiveProp != null && isActiveProp.CanWrite && isActiveProp.PropertyType == typeof(bool))
            {
                isActiveProp.SetValue(entity, false);
                SetTimestamp(type, entity, timestampUtc);
                return true;
            }

            var isArchivedProp = type.GetProperty("IsArchived", BindingFlags.Public | BindingFlags.Instance);
            if (isArchivedProp != null && isArchivedProp.CanWrite && isArchivedProp.PropertyType == typeof(bool))
            {
                isArchivedProp.SetValue(entity, true);
                SetTimestamp(type, entity, timestampUtc);
                return true;
            }

            return false;
        }

        private static void SetTimestamp(Type type, object entity, DateTime timestampUtc)
        {
            var updatedAt = type.GetProperty("UpdatedAt", BindingFlags.Public | BindingFlags.Instance);
            if (updatedAt == null || !updatedAt.CanWrite)
            {
                return;
            }

            if (updatedAt.PropertyType == typeof(DateTime))
            {
                updatedAt.SetValue(entity, timestampUtc);
                return;
            }

            if (updatedAt.PropertyType == typeof(DateTime?))
            {
                updatedAt.SetValue(entity, timestampUtc);
            }
        }
    }
}

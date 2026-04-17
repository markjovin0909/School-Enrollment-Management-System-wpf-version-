using System.Collections.Generic;
using School_Management_System.Models;
using School_Management_System.Services;

namespace School_Management_System.Interfaces
{
    internal interface IArchiveRecordService
    {
        IEnumerable<ArchiveRecord> GetAll();
        ArchiveRecord? GetById(long id);
        void Create(ArchiveRecord entity);
        void Update(ArchiveRecord entity);
        void Delete(long id);
        OperationResult<ArchiveRestoreImpactPreview> BuildRestoreImpactPreview(long archiveRecordId);
        OperationResult<bool> Restore(long archiveRecordId);
    }
}

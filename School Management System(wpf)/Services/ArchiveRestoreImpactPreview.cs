using System.Collections.Generic;
using System.Linq;

namespace School_Management_System.Services
{
    internal sealed class ArchiveDependencyImpactItem
    {
        public ArchiveDependencyImpactItem(string dependentEntity, string relation, int count)
        {
            DependentEntity = dependentEntity;
            Relation = relation;
            Count = count;
        }

        public string DependentEntity { get; }
        public string Relation { get; }
        public int Count { get; }
    }

    internal sealed class ArchiveRestoreImpactPreview
    {
        public long ArchiveRecordId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public long? OriginalEntityId { get; set; }
        public string RestoreStrategy { get; set; } = "UNKNOWN";
        public List<ArchiveDependencyImpactItem> Dependencies { get; } = new();
        public List<string> BlockingReasons { get; } = new();
        public List<string> Warnings { get; } = new();

        public bool CanProceed => BlockingReasons.Count == 0;
        public int TotalDependencyCount => Dependencies.Sum(x => x.Count);
    }
}

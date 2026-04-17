using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("archive_records")]
    public class ArchiveRecord : Interfaces.IBaseModel
    {
        public long Id { get; set; }

        [Column("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        [Column("original_entity_id")]
        public long? OriginalEntityId { get; set; }

        [Column("payload")]
        public string Payload { get; set; } = string.Empty;

        [Column("deleted_by_user_id")]
        public long? DeletedByUserId { get; set; }

        [Column("deleted_at")]
        public DateTime DeletedAt { get; set; }

        [Column("is_restored")]
        public bool IsRestored { get; set; }

        [Column("restored_by_user_id")]
        public long? RestoredByUserId { get; set; }

        [Column("restored_at")]
        public DateTime? RestoredAt { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        public User? DeletedByUser { get; set; }
        public User? RestoredByUser { get; set; }
    }
}

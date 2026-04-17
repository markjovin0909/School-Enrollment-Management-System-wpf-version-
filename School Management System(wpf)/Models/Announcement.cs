using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("announcements")]
    public class Announcement : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("posted_by_user_id")]
        public long PostedByUserId { get; set; }
        [Column("title")]
        public string Title { get; set; } = string.Empty;
        [Column("body")]
        public string Body { get; set; } = string.Empty;
        [Column("audience_type")]
        public AnnouncementAudienceType AudienceType { get; set; } = AnnouncementAudienceType.ALL;
        [Column("section_id")]
        public long? SectionId { get; set; }
        [Column("class_offering_id")]
        public long? ClassOfferingId { get; set; }
        [Column("posted_at")]
        public DateTime PostedAt { get; set; }
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public User? PostedByUser { get; set; }
        public Section? Section { get; set; }
        public ClassOffering? ClassOffering { get; set; }
    }
}

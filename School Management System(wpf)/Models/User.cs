using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace School_Management_System.Models
{
    [Table("users")]
    public class User : Interfaces.IBaseModel
    {
        public long Id { get; set; }
        [Column("username")]
        public string Username { get; set; } = string.Empty;
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;
        [Column("role")]
        public UserRole Role { get; set; } = UserRole.SUPERADMIN;
        [Column("can_login")]
        public bool CanLogin { get; set; } = true;
        [Column("status")]
        public UserStatus Status { get; set; } = UserStatus.ACTIVE;
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        public Teacher? Teacher { get; set; }
        public Student? Student { get; set; }
    }
}

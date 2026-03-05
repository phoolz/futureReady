using System;
using System.ComponentModel.DataAnnotations;

namespace Apiary.Models.School
{
    public class StudentAccountToken : TenantEntity
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// Returns true if token is not used, not expired, and not deleted
        /// </summary>
        public bool IsValid => UsedAt == null && ExpiresAt > DateTime.UtcNow && !IsDeleted;

        // Navigation property
        public Student? Student { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Apiary.Models.School
{
    public class FormToken : TenantEntity
    {
        [Required]
        public Guid PlacementId { get; set; }

        /// <summary>
        /// StudentId is set for parent form tokens (per-student), null for employer form tokens (placement-wide)
        /// </summary>
        public Guid? StudentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string FormType { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Email { get; set; }

        [Required]
        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// Returns true if token is not used, not expired, and not deleted
        /// </summary>
        public bool IsValid => UsedAt == null && ExpiresAt > DateTime.UtcNow && !IsDeleted;

        // Navigation properties
        public Placement? Placement { get; set; }
        public Student? Student { get; set; }
    }
}

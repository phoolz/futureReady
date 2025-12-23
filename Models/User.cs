using System;
using System.ComponentModel.DataAnnotations;

namespace FutureReady.Models
{
    public class User : TenantEntity
    {
        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = null!;

        [MaxLength(200)]
        public string? DisplayName { get; set; }

        [Required]
        [MaxLength(200)]
        [EmailAddress]
        public string Email { get; set; } = null!;

        // If you manage auth/passwords here, store a hash. Otherwise leave null and integrate with external identity.
        [MaxLength(500)]
        public string? PasswordHash { get; set; }

        public bool IsActive { get; set; } = true;

        // Optional: external identity subject/id to link to external auth providers
        public string? ExternalId { get; set; }
    }
}


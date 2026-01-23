using System;
using Microsoft.AspNetCore.Identity;

namespace FutureReady.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public Guid TenantId { get; set; }
        public string? DisplayName { get; set; }
        public bool IsActive { get; set; } = true;

        // Audit fields (from BaseEntity pattern)
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}

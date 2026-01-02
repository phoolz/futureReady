using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FutureReady.Models.School;

namespace FutureReady.Models
{
    public class Teacher : TenantEntity
    {
        [Required]
        public Guid UserId { get; set; }

        // Optional: primary school assignment
        public Guid? SchoolId { get; set; }

        [MaxLength(100)]
        public string? Title { get; set; }

        public DateTimeOffset? HireDate { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string? ExternalId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(SchoolId))]
        // Fully-qualify the School type to avoid ambiguity with the nested namespace "School" under FutureReady.Models
        public FutureReady.Models.School.School? School { get; set; }
    }
}

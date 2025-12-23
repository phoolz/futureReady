// csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace FutureReady.Models.Address
{
    public class Address : TenantEntity
    {
        [Required]
        [MaxLength(200)]
        public string Line1 { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? Line2 { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? State { get; set; }

        [Required]
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;
    }
}
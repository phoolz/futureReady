using System;
using System.ComponentModel.DataAnnotations;

namespace FutureReady.Models.School
{
    public class Supervisor : TenantEntity
    {
        [Required]
        public Guid CompanyId { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        [MaxLength(100)]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [MaxLength(200)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        // Navigation property
        public Company? Company { get; set; }

        // Computed property for display
        public string FullName => $"{FirstName} {LastName}";
    }
}

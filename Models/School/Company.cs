using System.ComponentModel.DataAnnotations;

namespace FutureReady.Models.School
{
    public class Company : TenantEntity
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(100)]
        public string? Industry { get; set; }

        // Physical Address
        [MaxLength(200)]
        [Display(Name = "Street Address")]
        public string? StreetAddress { get; set; }

        [MaxLength(200)]
        [Display(Name = "Street Address 2")]
        public string? StreetAddress2 { get; set; }

        [MaxLength(100)]
        public string? Suburb { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [MaxLength(20)]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        // Postal Address
        [MaxLength(200)]
        [Display(Name = "Postal Street Address")]
        public string? PostalStreetAddress { get; set; }

        [MaxLength(100)]
        [Display(Name = "Postal Suburb")]
        public string? PostalSuburb { get; set; }

        [MaxLength(100)]
        [Display(Name = "Postal City")]
        public string? PostalCity { get; set; }

        [MaxLength(50)]
        [Display(Name = "Postal State")]
        public string? PostalState { get; set; }

        [MaxLength(20)]
        [Display(Name = "Postal Postal Code")]
        public string? PostalPostalCode { get; set; }

        // Insurance & Experience
        [Display(Name = "Public Liability Insurance ($5M+)")]
        public bool PublicLiabilityInsurance5M { get; set; }

        [MaxLength(50)]
        [Display(Name = "Insurance Value")]
        public string? InsuranceValue { get; set; }

        [Display(Name = "Has Previous Work Experience Students")]
        public bool HasPreviousWorkExperienceStudents { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FutureReady.Models.School
{
    public class EmergencyContact : TenantEntity
    {
        [Required]
        public Guid StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public Student? Student { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        [MaxLength(20)]
        [Display(Name = "Mobile Number")]
        public string? MobileNumber { get; set; }

        [MaxLength(50)]
        public string? Relationship { get; set; }

        [Display(Name = "Primary Contact")]
        public bool IsPrimary { get; set; } = true;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}

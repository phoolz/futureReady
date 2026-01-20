using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FutureReady.Models;

namespace FutureReady.Models.School
{
    public class Student : TenantEntity
    {
        [Required]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        [MaxLength(100)]
        [Display(Name = "Preferred Name")]
        public string? PreferredName { get; set; }

        [Display(Name = "Date of Birth")]
        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(50)]
        [Display(Name = "Student Number")]
        public string? StudentNumber { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public Guid? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public StudentStatus Status { get; set; } = StudentStatus.Active;

        [MaxLength(200)]
        [Display(Name = "Guardian Name")]
        public string? GuardianName { get; set; }

        [MaxLength(256)]
        [EmailAddress]
        [Display(Name = "Guardian Email")]
        public string? GuardianEmail { get; set; }

        [MaxLength(20)]
        [Display(Name = "Guardian Phone")]
        public string? GuardianPhone { get; set; }

        [Required]
        public Guid CohortId { get; set; }

        [ForeignKey(nameof(CohortId))]
        public Cohort? Cohort { get; set; }

        [MaxLength(100)]
        [Display(Name = "Medicare Number")]
        public string? MedicareNumber { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string DisplayName => !string.IsNullOrEmpty(PreferredName) ? PreferredName : FirstName;
    }
}

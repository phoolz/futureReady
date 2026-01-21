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

        [MaxLength(20)]
        [Display(Name = "Student Type")]
        public string? StudentType { get; set; }

        [MaxLength(20)]
        [Display(Name = "Year Level")]
        public string? YearLevel { get; set; }

        [Display(Name = "Graduation Year")]
        public int? GraduationYear { get; set; }

        [MaxLength(100)]
        [Display(Name = "Medicare Number")]
        public string? MedicareNumber { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [NotMapped]
        public string DisplayName => !string.IsNullOrEmpty(PreferredName) ? PreferredName : FirstName;
    }
}

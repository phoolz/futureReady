using System;
using System.ComponentModel.DataAnnotations;

namespace Apiary.Models.StudentActivation
{
    public class StudentActivationDto
    {
        // Read-only (displayed for confirmation)
        public Guid StudentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Account creation
        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Editable profile fields
        [MaxLength(100)]
        public string? PreferredName { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(50)]
        public string? StudentNumber { get; set; }

        [MaxLength(20)]
        [Phone]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? StudentType { get; set; }

        [MaxLength(20)]
        public string? YearLevel { get; set; }

        public int? GraduationYear { get; set; }

        [MaxLength(100)]
        public string? MedicareNumber { get; set; }
    }
}

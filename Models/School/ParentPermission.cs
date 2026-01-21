using System;
using System.ComponentModel.DataAnnotations;

namespace FutureReady.Models.School
{
    public class ParentPermission : TenantEntity
    {
        [Required]
        public Guid PlacementId { get; set; }

        // Transport details
        [MaxLength(50)]
        [Display(Name = "Transport Method")]
        public string? TransportMethod { get; set; }  // 'public', 'private_car', 'combination'

        [Display(Name = "Public Transport Details")]
        public string? PublicTransportDetails { get; set; }

        [MaxLength(200)]
        [Display(Name = "Driver Name")]
        public string? DriverName { get; set; }

        [MaxLength(50)]
        [Display(Name = "Driver Contact Number")]
        public string? DriverContactNumber { get; set; }  // PII: encrypt later

        // Teacher pre-visit
        [Display(Name = "Request Teacher Pre-visit")]
        public bool RequestTeacherPrevisit { get; set; } = false;

        // Medical info to share with employer
        [Display(Name = "Share Medical with Employer")]
        public bool ShareMedicalWithEmployer { get; set; } = false;

        [Display(Name = "Medical Notes for Employer")]
        public string? MedicalNotesForEmployer { get; set; }  // PII: encrypt later

        // Consent
        [MaxLength(100)]
        [Display(Name = "Parent First Name")]
        public string? ParentFirstName { get; set; }

        [MaxLength(100)]
        [Display(Name = "Parent Last Name")]
        public string? ParentLastName { get; set; }

        [Display(Name = "Consent Given")]
        public bool ConsentGiven { get; set; } = false;

        [Display(Name = "Consent Date")]
        public DateOnly? ConsentDate { get; set; }

        // Navigation property
        public Placement? Placement { get; set; }
    }
}

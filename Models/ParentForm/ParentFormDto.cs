using System;
using System.ComponentModel.DataAnnotations;

namespace FutureReady.Models.ParentForm
{
    public class ParentFormDto
    {
        public Guid PlacementId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public int CurrentStep { get; set; } = 1;
        public DateTime? LastSavedAt { get; set; }

        // Nested DTOs for each step
        public StudentDetailsDto StudentDetails { get; set; } = new();
        public EmergencyContactDto EmergencyContact { get; set; } = new();
        public WorkplaceDetailsDto WorkplaceDetails { get; set; } = new();
        public TransportDto Transport { get; set; } = new();
        public MedicalDetailsDto MedicalDetails { get; set; } = new();
        public ConsentDto Consent { get; set; } = new();
    }

    // Step 1: Student Details
    public class StudentDetailsDto
    {
        [Required(ErrorMessage = "Student type is required")]
        [MaxLength(20)]
        [Display(Name = "Student Type")]
        public string StudentType { get; set; } = string.Empty;  // "day" or "boarding"

        [Required(ErrorMessage = "Mobile number is required")]
        [MaxLength(20)]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Student Mobile Number")]
        public string MobileNumber { get; set; } = string.Empty;
    }

    // Step 2: Emergency Contact
    public class EmergencyContactDto
    {
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile number is required")]
        [MaxLength(20)]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Mobile Number")]
        public string MobileNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Relationship is required")]
        [MaxLength(50)]
        [Display(Name = "Relationship to Student")]
        public string Relationship { get; set; } = string.Empty;
    }

    // Step 3: Workplace Details
    public class WorkplaceDetailsDto
    {
        // Flag to indicate if company is already set (fields become read-only)
        public bool IsCompanyPreset { get; set; }

        [Required(ErrorMessage = "Company name is required")]
        [MaxLength(200)]
        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact first name is required")]
        [MaxLength(100)]
        [Display(Name = "Contact First Name")]
        public string ContactFirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact last name is required")]
        [MaxLength(100)]
        [Display(Name = "Contact Last Name")]
        public string ContactLastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact email is required")]
        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Contact Email")]
        public string ContactEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact phone is required")]
        [MaxLength(20)]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Contact Phone")]
        public string ContactPhone { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Industry")]
        public string? Industry { get; set; }

        [Required(ErrorMessage = "Street address is required")]
        [MaxLength(200)]
        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Street Address 2")]
        public string? StreetAddress2 { get; set; }

        [Required(ErrorMessage = "City is required")]
        [MaxLength(100)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        [MaxLength(50)]
        [Display(Name = "State")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required")]
        [MaxLength(20)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;
    }

    // Step 4: Transport
    public class TransportDto
    {
        [Required(ErrorMessage = "Transport method is required")]
        [MaxLength(50)]
        [Display(Name = "Transport Method")]
        public string TransportMethod { get; set; } = string.Empty;  // "public", "private_car", "combination"

        [MaxLength(500)]
        [Display(Name = "Public Transport Details")]
        public string? PublicTransportDetails { get; set; }

        [MaxLength(200)]
        [Display(Name = "Driver Name")]
        public string? DriverName { get; set; }

        [MaxLength(50)]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Driver Contact Number")]
        public string? DriverContactNumber { get; set; }
    }

    // Step 5: Medical Details
    public class MedicalDetailsDto
    {
        // Asthma
        [Display(Name = "Asthma")]
        public bool HasAsthma { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Asthma Details")]
        public string? AsthmaDetails { get; set; }

        // Diabetes
        [Display(Name = "Diabetes")]
        public bool HasDiabetes { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Diabetes Details")]
        public string? DiabetesDetails { get; set; }

        // Epilepsy
        [Display(Name = "Epilepsy")]
        public bool HasEpilepsy { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Epilepsy Details")]
        public string? EpilepsyDetails { get; set; }

        // Allergies
        [Display(Name = "Allergies")]
        public bool HasAllergies { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Allergies Details")]
        public string? AllergiesDetails { get; set; }

        // Learning Difficulties
        [Display(Name = "Learning Difficulties")]
        public bool HasLearningDifficulties { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Learning Difficulties Details")]
        public string? LearningDifficultiesDetails { get; set; }

        // Medication
        [Display(Name = "Regular Medication")]
        public bool HasMedication { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Medication Details")]
        public string? MedicationDetails { get; set; }

        // Other
        [Display(Name = "Other Condition")]
        public bool HasOther { get; set; }

        [MaxLength(2000)]
        [Display(Name = "Other Details")]
        public string? OtherDetails { get; set; }
    }

    // Step 6: Consent
    public class ConsentDto
    {
        [Display(Name = "Share Medical Information with Employer")]
        public bool ShareMedicalWithEmployer { get; set; }

        [Display(Name = "Request Teacher Pre-visit")]
        public bool RequestTeacherPrevisit { get; set; }

        [Required(ErrorMessage = "Parent first name is required")]
        [MaxLength(100)]
        [Display(Name = "Parent/Guardian First Name")]
        public string ParentFirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Parent last name is required")]
        [MaxLength(100)]
        [Display(Name = "Parent/Guardian Last Name")]
        public string ParentLastName { get; set; } = string.Empty;

        [Display(Name = "Consent Date")]
        public DateOnly ConsentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required(ErrorMessage = "You must provide consent to submit this form")]
        [Display(Name = "I consent to this work experience placement")]
        public bool ConsentGiven { get; set; }
    }
}

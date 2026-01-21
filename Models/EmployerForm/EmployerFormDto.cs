using System;
using System.ComponentModel.DataAnnotations;

namespace FutureReady.Models.EmployerForm
{
    public class EmployerFormDto
    {
        public Guid PlacementId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string SchoolName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;

        public WorkplaceDetailsDto WorkplaceDetails { get; set; } = new();
        public SupervisorDetailsDto SupervisorDetails { get; set; } = new();
        public InsuranceDto Insurance { get; set; } = new();
        public OhsDto Ohs { get; set; } = new();
        public GeneralTravelDto GeneralTravel { get; set; } = new();
        public HazardsAppendixDto HazardsAppendix { get; set; } = new();

        public int CurrentStep { get; set; } = 1;
        public DateTime? LastSavedAt { get; set; }
    }

    // Step 1: Workplace Details
    public class WorkplaceDetailsDto
    {
        [Required(ErrorMessage = "Street address is required")]
        [MaxLength(200)]
        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "Street Address 2")]
        public string? StreetAddress2 { get; set; }

        [Required(ErrorMessage = "Suburb is required")]
        [MaxLength(100)]
        public string Suburb { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        [MaxLength(50)]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required")]
        [MaxLength(20)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "Dress Code Requirements")]
        public string? DressCode { get; set; }

        [Required(ErrorMessage = "Work start time is required")]
        [MaxLength(10)]
        [Display(Name = "Work Start Time")]
        public string WorkStartTime { get; set; } = string.Empty;

        [Required(ErrorMessage = "Work end time is required")]
        [MaxLength(10)]
        [Display(Name = "Work End Time")]
        public string WorkEndTime { get; set; } = string.Empty;
    }

    // Step 2: Supervisor Details
    public class SupervisorDetailsDto
    {
        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [MaxLength(200)]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [MaxLength(20)]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string Phone { get; set; } = string.Empty;
    }

    // Step 3: Insurance
    public class InsuranceDto
    {
        [Required(ErrorMessage = "Please indicate if you have public liability insurance of at least $5M")]
        [Display(Name = "Do you have Public Liability Insurance of at least $5 million?")]
        public bool? HasPublicLiabilityInsurance5M { get; set; }

        [MaxLength(50)]
        [Display(Name = "Insurance Value")]
        public string? InsuranceValue { get; set; }

        [Required(ErrorMessage = "Please indicate if you have had previous work experience students")]
        [Display(Name = "Have you had previous work experience students?")]
        public bool? HasPreviousWorkExperienceStudents { get; set; }
    }

    // Step 4: OHS (Occupational Health & Safety)
    public class OhsDto
    {
        [Required(ErrorMessage = "Please indicate if you have an OHS policy")]
        [Display(Name = "Do you have an OHS policy?")]
        public bool? HasOhsPolicy { get; set; }

        [Required(ErrorMessage = "Please indicate if you have an induction program")]
        [Display(Name = "Do you have an induction program for new workers?")]
        public bool? HasInductionProgram { get; set; }

        [MaxLength(500)]
        [Display(Name = "How will the student be briefed on safety procedures?")]
        public string? SafetyBriefingMethod { get; set; }

        [Required(ErrorMessage = "Please indicate if there are any obvious hazards")]
        [Display(Name = "Are there any obvious hazards in the workplace?")]
        public bool? HasObviousHazards { get; set; }

        [Display(Name = "Please provide details of the hazards")]
        public string? HazardDetails { get; set; }

        [Display(Name = "What training will be provided to prevent injury?")]
        public string? InjuryPreventionTraining { get; set; }

        [Required(ErrorMessage = "Please indicate if hazard reporting instruction will be provided")]
        [Display(Name = "Will the student be instructed on how to report hazards?")]
        public bool? ProvidesHazardReportingInstruction { get; set; }

        [Required(ErrorMessage = "Please indicate if you have emergency procedures")]
        [Display(Name = "Do you have documented emergency procedures?")]
        public bool? HasEmergencyProcedures { get; set; }

        [Required(ErrorMessage = "Please indicate if fire extinguishers are checked")]
        [Display(Name = "Are fire extinguishers checked and maintained?")]
        public bool? HasFireExtinguishersChecked { get; set; }

        [Required(ErrorMessage = "Please indicate if you have a first aid kit")]
        [Display(Name = "Is there a first aid kit available?")]
        public bool? HasFirstAidKit { get; set; }

        [Required(ErrorMessage = "Please indicate if amenities are safe and accessible")]
        [Display(Name = "Are amenities (toilets, eating areas) safe and accessible?")]
        public bool? HasSafeAmenities { get; set; }
    }

    // Step 5: General & Travel
    public class GeneralTravelDto
    {
        [Required(ErrorMessage = "Please indicate if staff will be informed of the student")]
        [Display(Name = "Will staff be informed of the student's placement?")]
        public bool? StaffInformedOfStudent { get; set; }

        [Required(ErrorMessage = "Please indicate if staff meet Working with Children requirements")]
        [Display(Name = "Do all relevant staff meet Working with Children Check requirements?")]
        public bool? StaffMeetWorkingWithChildrenRequirements { get; set; }

        [Required(ErrorMessage = "Please indicate if additional information is required")]
        [Display(Name = "Is any additional information required from the student or school?")]
        public bool? AdditionalInfoRequired { get; set; }

        [Display(Name = "Please provide details")]
        public string? AdditionalInfoDetails { get; set; }

        [Required(ErrorMessage = "Please indicate if vehicle travel is required")]
        [Display(Name = "Will the student be required to travel in a vehicle during the placement?")]
        public bool? RequiresVehicleTravel { get; set; }

        [Display(Name = "Please provide vehicle travel details")]
        public string? VehicleDetails { get; set; }

        [MaxLength(500)]
        [Display(Name = "Driver's experience")]
        public string? DriverExperience { get; set; }

        [MaxLength(100)]
        [Display(Name = "Driver's licence type")]
        public string? LicenceType { get; set; }
    }

    // Step 6: Hazards Appendix
    public class HazardsAppendixDto
    {
        [Display(Name = "Chemical hazards")]
        public bool HasChemicalHazards { get; set; }

        [Display(Name = "Chemical hazard details")]
        public string? ChemicalDetails { get; set; }

        [Display(Name = "Plant/Machinery hazards")]
        public bool HasPlantMachineryHazards { get; set; }

        [Display(Name = "Plant/Machinery hazard details")]
        public string? PlantMachineryDetails { get; set; }

        [Display(Name = "Biological hazards")]
        public bool HasBiologicalHazards { get; set; }

        [Display(Name = "Biological hazard details")]
        public string? BiologicalDetails { get; set; }

        [Display(Name = "Ergonomic hazards")]
        public bool HasErgonomicHazards { get; set; }

        [Display(Name = "Ergonomic hazard details")]
        public string? ErgonomicDetails { get; set; }

        [Display(Name = "Additional hazard information")]
        public string? AdditionalDetails { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace FutureReady.Models.School
{
    public class Placement : TenantEntity
    {
        [Required]
        public Guid StudentId { get; set; }

        public Guid? CompanyId { get; set; }

        public Guid? SupervisorId { get; set; }

        public int? Year { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "draft";  // 'draft', 'pending_parent', 'pending_employer', 'confirmed'

        // Placement details
        [MaxLength(500)]
        [Display(Name = "Dress Requirement")]
        public string? DressRequirement { get; set; }

        [MaxLength(10)]
        [Display(Name = "Work Start Time")]
        public string? WorkStartTime { get; set; }

        [MaxLength(10)]
        [Display(Name = "Work End Time")]
        public string? WorkEndTime { get; set; }

        // OHS
        [Display(Name = "Has OHS Policy")]
        public bool? HasOhsPolicy { get; set; }

        [Display(Name = "Has Induction Program")]
        public bool? HasInductionProgram { get; set; }

        [MaxLength(500)]
        [Display(Name = "Safety Briefing Method")]
        public string? SafetyBriefingMethod { get; set; }

        [Display(Name = "Has Obvious Hazards")]
        public bool? HasObviousHazards { get; set; }

        [Display(Name = "Hazard Details")]
        public string? HazardDetails { get; set; }

        [Display(Name = "Injury Prevention Training")]
        public string? InjuryPreventionTraining { get; set; }

        [Display(Name = "Provides Hazard Reporting Instruction")]
        public bool? ProvidesHazardReportingInstruction { get; set; }

        [Display(Name = "Has Emergency Procedures")]
        public bool? HasEmergencyProcedures { get; set; }

        [Display(Name = "Has Fire Extinguishers Checked")]
        public bool? HasFireExtinguishersChecked { get; set; }

        [Display(Name = "Has First Aid Kit")]
        public bool? HasFirstAidKit { get; set; }

        [Display(Name = "Has Safe Amenities")]
        public bool? HasSafeAmenities { get; set; }

        // General
        [Display(Name = "Staff Informed of Student")]
        public bool? StaffInformedOfStudent { get; set; }

        [Display(Name = "Staff Meet Working with Children Requirements")]
        public bool? StaffMeetWorkingWithChildrenRequirements { get; set; }

        [Display(Name = "Additional Info Required")]
        public bool? AdditionalInfoRequired { get; set; }

        [Display(Name = "Additional Info Details")]
        public string? AdditionalInfoDetails { get; set; }

        // Employer vehicle travel
        [Display(Name = "Employer Requires Vehicle Travel")]
        public bool? EmployerRequiresVehicleTravel { get; set; }

        [Display(Name = "Employer Vehicle Details")]
        public string? EmployerVehicleDetails { get; set; }

        [MaxLength(500)]
        [Display(Name = "Employer Driver Experience")]
        public string? EmployerDriverExperience { get; set; }

        [MaxLength(100)]
        [Display(Name = "Employer Licence Type")]
        public string? EmployerLicenceType { get; set; }

        // Hazards appendix
        [Display(Name = "Has Chemical Hazards")]
        public bool HasChemicalHazards { get; set; } = false;

        [Display(Name = "Chemical Details")]
        public string? ChemicalDetails { get; set; }

        [Display(Name = "Has Plant/Machinery Hazards")]
        public bool HasPlantMachineryHazards { get; set; } = false;

        [Display(Name = "Plant/Machinery Details")]
        public string? PlantMachineryDetails { get; set; }

        [Display(Name = "Has Biological Hazards")]
        public bool HasBiologicalHazards { get; set; } = false;

        [Display(Name = "Biological Details")]
        public string? BiologicalDetails { get; set; }

        [Display(Name = "Has Ergonomic Hazards")]
        public bool HasErgonomicHazards { get; set; } = false;

        [Display(Name = "Ergonomic Details")]
        public string? ErgonomicDetails { get; set; }

        [Display(Name = "Hazards Additional Details")]
        public string? HazardsAdditionalDetails { get; set; }

        // Submission timestamps
        [Display(Name = "Employer Submitted At")]
        public DateTime? EmployerSubmittedAt { get; set; }

        [Display(Name = "Parent Submitted At")]
        public DateTime? ParentSubmittedAt { get; set; }

        // Navigation properties
        public Student? Student { get; set; }
        public Company? Company { get; set; }
        public Supervisor? Supervisor { get; set; }
    }
}

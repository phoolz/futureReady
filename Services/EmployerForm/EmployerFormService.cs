using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.EmployerForm;
using FutureReady.Services.FormTokens;

namespace FutureReady.Services.EmployerForm
{
    public class EmployerFormService : IEmployerFormService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFormTokenService _formTokenService;

        public EmployerFormService(ApplicationDbContext context, IFormTokenService formTokenService)
        {
            _context = context;
            _formTokenService = formTokenService;
        }

        public async Task<EmployerFormDto?> InitializeFormAsync(string token)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid)
            {
                return null;
            }

            // Get placement with related data (bypass tenant filter for public form)
            var placement = await _context.Placements
                .IgnoreQueryFilters()
                .Include(p => p.Student)
                .Include(p => p.Company)
                .Include(p => p.Supervisor)
                .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (placement == null)
            {
                return null;
            }

            // Get school info
            var school = await _context.Schools
                .IgnoreQueryFilters()
                .Where(s => s.Id == placement.TenantId && !s.IsDeleted)
                .FirstOrDefaultAsync();

            var dto = new EmployerFormDto
            {
                PlacementId = placement.Id,
                StudentName = placement.Student?.FullName ?? "Unknown Student",
                SchoolName = school?.Name ?? "Unknown School",
                CompanyName = placement.Company?.Name ?? "Unknown Company",
                CurrentStep = 1
            };

            // Pre-fill from existing data
            if (placement.Company != null)
            {
                dto.WorkplaceDetails = new WorkplaceDetailsDto
                {
                    StreetAddress = placement.Company.StreetAddress ?? string.Empty,
                    StreetAddress2 = placement.Company.StreetAddress2,
                    Suburb = placement.Company.Suburb ?? string.Empty,
                    City = placement.Company.City ?? string.Empty,
                    State = placement.Company.State ?? string.Empty,
                    PostalCode = placement.Company.PostalCode ?? string.Empty,
                    DressCode = placement.DressRequirement,
                    WorkStartTime = placement.WorkStartTime ?? string.Empty,
                    WorkEndTime = placement.WorkEndTime ?? string.Empty
                };

                dto.Insurance = new InsuranceDto
                {
                    HasPublicLiabilityInsurance5M = placement.Company.PublicLiabilityInsurance5M ? true : null,
                    InsuranceValue = placement.Company.InsuranceValue,
                    HasPreviousWorkExperienceStudents = placement.Company.HasPreviousWorkExperienceStudents ? true : null
                };
            }

            if (placement.Supervisor != null)
            {
                dto.SupervisorDetails = new SupervisorDetailsDto
                {
                    FirstName = placement.Supervisor.FirstName,
                    LastName = placement.Supervisor.LastName,
                    JobTitle = placement.Supervisor.JobTitle,
                    Email = placement.Supervisor.Email ?? string.Empty,
                    Phone = placement.Supervisor.Phone ?? string.Empty
                };
            }

            // Pre-fill OHS from placement
            dto.Ohs = new OhsDto
            {
                HasOhsPolicy = placement.HasOhsPolicy,
                HasInductionProgram = placement.HasInductionProgram,
                SafetyBriefingMethod = placement.SafetyBriefingMethod,
                HasObviousHazards = placement.HasObviousHazards,
                HazardDetails = placement.HazardDetails,
                InjuryPreventionTraining = placement.InjuryPreventionTraining,
                ProvidesHazardReportingInstruction = placement.ProvidesHazardReportingInstruction,
                HasEmergencyProcedures = placement.HasEmergencyProcedures,
                HasFireExtinguishersChecked = placement.HasFireExtinguishersChecked,
                HasFirstAidKit = placement.HasFirstAidKit,
                HasSafeAmenities = placement.HasSafeAmenities
            };

            // Pre-fill General/Travel from placement
            dto.GeneralTravel = new GeneralTravelDto
            {
                StaffInformedOfStudent = placement.StaffInformedOfStudent,
                StaffMeetWorkingWithChildrenRequirements = placement.StaffMeetWorkingWithChildrenRequirements,
                AdditionalInfoRequired = placement.AdditionalInfoRequired,
                AdditionalInfoDetails = placement.AdditionalInfoDetails,
                RequiresVehicleTravel = placement.EmployerRequiresVehicleTravel,
                VehicleDetails = placement.EmployerVehicleDetails,
                DriverExperience = placement.EmployerDriverExperience,
                LicenceType = placement.EmployerLicenceType
            };

            // Pre-fill Hazards Appendix from placement
            dto.HazardsAppendix = new HazardsAppendixDto
            {
                HasChemicalHazards = placement.HasChemicalHazards,
                ChemicalDetails = placement.ChemicalDetails,
                HasPlantMachineryHazards = placement.HasPlantMachineryHazards,
                PlantMachineryDetails = placement.PlantMachineryDetails,
                HasBiologicalHazards = placement.HasBiologicalHazards,
                BiologicalDetails = placement.BiologicalDetails,
                HasErgonomicHazards = placement.HasErgonomicHazards,
                ErgonomicDetails = placement.ErgonomicDetails,
                AdditionalDetails = placement.HazardsAdditionalDetails
            };

            return dto;
        }

        public async Task<bool> SubmitFormAsync(string token, EmployerFormDto formData)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid)
            {
                return false;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get placement (bypass tenant filter)
                var placement = await _context.Placements
                    .IgnoreQueryFilters()
                    .Include(p => p.Company)
                    .Include(p => p.Supervisor)
                    .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (placement == null)
                {
                    return false;
                }

                // Update Company
                if (placement.Company != null)
                {
                    placement.Company.StreetAddress = formData.WorkplaceDetails.StreetAddress;
                    placement.Company.StreetAddress2 = formData.WorkplaceDetails.StreetAddress2;
                    placement.Company.Suburb = formData.WorkplaceDetails.Suburb;
                    placement.Company.City = formData.WorkplaceDetails.City;
                    placement.Company.State = formData.WorkplaceDetails.State;
                    placement.Company.PostalCode = formData.WorkplaceDetails.PostalCode;
                    placement.Company.PublicLiabilityInsurance5M = formData.Insurance.HasPublicLiabilityInsurance5M ?? false;
                    placement.Company.InsuranceValue = formData.Insurance.InsuranceValue;
                    placement.Company.HasPreviousWorkExperienceStudents = formData.Insurance.HasPreviousWorkExperienceStudents ?? false;
                }

                // Update Supervisor
                if (placement.Supervisor != null)
                {
                    placement.Supervisor.FirstName = formData.SupervisorDetails.FirstName;
                    placement.Supervisor.LastName = formData.SupervisorDetails.LastName;
                    placement.Supervisor.JobTitle = formData.SupervisorDetails.JobTitle;
                    placement.Supervisor.Email = formData.SupervisorDetails.Email;
                    placement.Supervisor.Phone = formData.SupervisorDetails.Phone;
                }

                // Update Placement
                placement.DressRequirement = formData.WorkplaceDetails.DressCode;
                placement.WorkStartTime = formData.WorkplaceDetails.WorkStartTime;
                placement.WorkEndTime = formData.WorkplaceDetails.WorkEndTime;

                // OHS
                placement.HasOhsPolicy = formData.Ohs.HasOhsPolicy;
                placement.HasInductionProgram = formData.Ohs.HasInductionProgram;
                placement.SafetyBriefingMethod = formData.Ohs.SafetyBriefingMethod;
                placement.HasObviousHazards = formData.Ohs.HasObviousHazards;
                placement.HazardDetails = formData.Ohs.HazardDetails;
                placement.InjuryPreventionTraining = formData.Ohs.InjuryPreventionTraining;
                placement.ProvidesHazardReportingInstruction = formData.Ohs.ProvidesHazardReportingInstruction;
                placement.HasEmergencyProcedures = formData.Ohs.HasEmergencyProcedures;
                placement.HasFireExtinguishersChecked = formData.Ohs.HasFireExtinguishersChecked;
                placement.HasFirstAidKit = formData.Ohs.HasFirstAidKit;
                placement.HasSafeAmenities = formData.Ohs.HasSafeAmenities;

                // General/Travel
                placement.StaffInformedOfStudent = formData.GeneralTravel.StaffInformedOfStudent;
                placement.StaffMeetWorkingWithChildrenRequirements = formData.GeneralTravel.StaffMeetWorkingWithChildrenRequirements;
                placement.AdditionalInfoRequired = formData.GeneralTravel.AdditionalInfoRequired;
                placement.AdditionalInfoDetails = formData.GeneralTravel.AdditionalInfoDetails;
                placement.EmployerRequiresVehicleTravel = formData.GeneralTravel.RequiresVehicleTravel;
                placement.EmployerVehicleDetails = formData.GeneralTravel.VehicleDetails;
                placement.EmployerDriverExperience = formData.GeneralTravel.DriverExperience;
                placement.EmployerLicenceType = formData.GeneralTravel.LicenceType;

                // Hazards Appendix
                placement.HasChemicalHazards = formData.HazardsAppendix.HasChemicalHazards;
                placement.ChemicalDetails = formData.HazardsAppendix.ChemicalDetails;
                placement.HasPlantMachineryHazards = formData.HazardsAppendix.HasPlantMachineryHazards;
                placement.PlantMachineryDetails = formData.HazardsAppendix.PlantMachineryDetails;
                placement.HasBiologicalHazards = formData.HazardsAppendix.HasBiologicalHazards;
                placement.BiologicalDetails = formData.HazardsAppendix.BiologicalDetails;
                placement.HasErgonomicHazards = formData.HazardsAppendix.HasErgonomicHazards;
                placement.ErgonomicDetails = formData.HazardsAppendix.ErgonomicDetails;
                placement.HazardsAdditionalDetails = formData.HazardsAppendix.AdditionalDetails;

                // Set submission timestamp
                placement.EmployerSubmittedAt = DateTime.UtcNow;

                // Update status to pending_parent if it was pending_employer
                if (placement.Status == "pending_employer")
                {
                    placement.Status = "pending_parent";
                }

                await _context.SaveChangesAsync();

                // Mark token as used
                await _formTokenService.MarkAsUsedAsync(token);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}

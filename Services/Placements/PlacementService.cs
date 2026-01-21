using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;

namespace FutureReady.Services.Placements
{
    public class PlacementService : IPlacementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public PlacementService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<List<Placement>> GetAllAsync(Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.Placements
                .AsNoTracking()
                .Include(p => p.Student)
                .Include(p => p.Company)
                .Include(p => p.Supervisor)
                .AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(p => p.TenantId == tenantId.Value);

            return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<List<Placement>> GetByStudentIdAsync(Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.Placements
                .AsNoTracking()
                .Include(p => p.Company)
                .Include(p => p.Supervisor)
                .Where(p => p.StudentId == studentId);

            if (tenantId.HasValue)
                query = query.Where(p => p.TenantId == tenantId.Value);

            return await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<List<Placement>> GetByCompanyIdAsync(Guid companyId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.Placements
                .AsNoTracking()
                .Include(p => p.Student)
                .Include(p => p.Supervisor)
                .Where(p => p.CompanyId == companyId);

            if (tenantId.HasValue)
                query = query.Where(p => p.TenantId == tenantId.Value);

            return await query.OrderByDescending(p => p.Year).ThenByDescending(p => p.CreatedAt).ToListAsync();
        }

        public async Task<Placement?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Placements
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id && (!tenantId.HasValue || p.TenantId == tenantId.Value));
        }

        public async Task<Placement?> GetByIdWithDetailsAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Placements
                .AsNoTracking()
                .Include(p => p.Student)
                .Include(p => p.Company)
                .Include(p => p.Supervisor)
                .FirstOrDefaultAsync(p => p.Id == id && (!tenantId.HasValue || p.TenantId == tenantId.Value));
        }

        public async Task CreateAsync(Placement placement, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating a placement.");

            placement.TenantId = tenantId.Value;

            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Placement placement, byte[]? rowVersion = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var existing = await _context.Placements
                .FirstOrDefaultAsync(p => p.Id == placement.Id && (!tenantId.HasValue || p.TenantId == tenantId.Value));

            if (existing == null)
                throw new InvalidOperationException("Placement not found");

            existing.StudentId = placement.StudentId;
            existing.CompanyId = placement.CompanyId;
            existing.SupervisorId = placement.SupervisorId;
            existing.Year = placement.Year;
            existing.Status = placement.Status;
            existing.DressRequirement = placement.DressRequirement;
            existing.WorkStartTime = placement.WorkStartTime;
            existing.WorkEndTime = placement.WorkEndTime;
            existing.HasOhsPolicy = placement.HasOhsPolicy;
            existing.HasInductionProgram = placement.HasInductionProgram;
            existing.SafetyBriefingMethod = placement.SafetyBriefingMethod;
            existing.HasObviousHazards = placement.HasObviousHazards;
            existing.HazardDetails = placement.HazardDetails;
            existing.InjuryPreventionTraining = placement.InjuryPreventionTraining;
            existing.ProvidesHazardReportingInstruction = placement.ProvidesHazardReportingInstruction;
            existing.HasEmergencyProcedures = placement.HasEmergencyProcedures;
            existing.HasFireExtinguishersChecked = placement.HasFireExtinguishersChecked;
            existing.HasFirstAidKit = placement.HasFirstAidKit;
            existing.HasSafeAmenities = placement.HasSafeAmenities;
            existing.StaffInformedOfStudent = placement.StaffInformedOfStudent;
            existing.StaffMeetWorkingWithChildrenRequirements = placement.StaffMeetWorkingWithChildrenRequirements;
            existing.AdditionalInfoRequired = placement.AdditionalInfoRequired;
            existing.AdditionalInfoDetails = placement.AdditionalInfoDetails;
            existing.EmployerRequiresVehicleTravel = placement.EmployerRequiresVehicleTravel;
            existing.EmployerVehicleDetails = placement.EmployerVehicleDetails;
            existing.EmployerDriverExperience = placement.EmployerDriverExperience;
            existing.EmployerLicenceType = placement.EmployerLicenceType;
            existing.HasChemicalHazards = placement.HasChemicalHazards;
            existing.ChemicalDetails = placement.ChemicalDetails;
            existing.HasPlantMachineryHazards = placement.HasPlantMachineryHazards;
            existing.PlantMachineryDetails = placement.PlantMachineryDetails;
            existing.HasBiologicalHazards = placement.HasBiologicalHazards;
            existing.BiologicalDetails = placement.BiologicalDetails;
            existing.HasErgonomicHazards = placement.HasErgonomicHazards;
            existing.ErgonomicDetails = placement.ErgonomicDetails;
            existing.HazardsAdditionalDetails = placement.HazardsAdditionalDetails;
            existing.EmployerSubmittedAt = placement.EmployerSubmittedAt;
            existing.ParentSubmittedAt = placement.ParentSubmittedAt;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var placement = await _context.Placements
                .FirstOrDefaultAsync(p => p.Id == id && (!tenantId.HasValue || p.TenantId == tenantId.Value));

            if (placement != null)
            {
                _context.Placements.Remove(placement);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Placements
                .AnyAsync(p => p.Id == id && (!tenantId.HasValue || p.TenantId == tenantId.Value));
        }
    }
}

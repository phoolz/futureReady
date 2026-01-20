using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;
using FutureReady.Services;

namespace FutureReady.Services.Cohorts
{
    public class CohortService : ICohortService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public CohortService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<List<Cohort>> GetAllAsync(Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.Cohorts
                .Include(c => c.School)
                .AsNoTracking()
                .AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(c => c.TenantId == tenantId.Value);

            return await query.ToListAsync();
        }

        public async Task<Cohort?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Cohorts
                .Include(c => c.School)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && (!tenantId.HasValue || c.TenantId == tenantId.Value));
        }

        public async Task CreateAsync(Cohort cohort, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating a cohort.");

            cohort.TenantId = tenantId.Value;
            cohort.SchoolId = tenantId.Value; // the school equals tenant id in this app

            _context.Add(cohort);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Cohort cohort, byte[]? rowVersion = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var existing = await _context.Cohorts.FirstOrDefaultAsync(c => c.Id == cohort.Id && (!tenantId.HasValue || c.TenantId == tenantId.Value));
            if (existing == null) throw new InvalidOperationException("Cohort not found");

            // Do not allow changing SchoolId; it must remain the tenant's school
            existing.Name = cohort.Name;
            existing.GraduationYear = cohort.GraduationYear;
            existing.GraduationMonth = cohort.GraduationMonth;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var cohort = await _context.Cohorts.FirstOrDefaultAsync(c => c.Id == id && (!tenantId.HasValue || c.TenantId == tenantId.Value));
            if (cohort != null)
            {
                _context.Cohorts.Remove(cohort);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Cohorts.AnyAsync(c => c.Id == id && (!tenantId.HasValue || c.TenantId == tenantId.Value));
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;

namespace FutureReady.Services.Supervisors
{
    public class SupervisorService : ISupervisorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public SupervisorService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<List<Supervisor>> GetAllAsync(Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.Supervisors
                .AsNoTracking()
                .Include(s => s.Company)
                .AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(s => s.TenantId == tenantId.Value);

            return await query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync();
        }

        public async Task<List<Supervisor>> GetByCompanyAsync(Guid companyId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.Supervisors
                .AsNoTracking()
                .Include(s => s.Company)
                .Where(s => s.CompanyId == companyId);

            if (tenantId.HasValue)
                query = query.Where(s => s.TenantId == tenantId.Value);

            return await query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName).ToListAsync();
        }

        public async Task<Supervisor?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Supervisors
                .AsNoTracking()
                .Include(s => s.Company)
                .FirstOrDefaultAsync(s => s.Id == id && (!tenantId.HasValue || s.TenantId == tenantId.Value));
        }

        public async Task CreateAsync(Supervisor supervisor, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating a supervisor.");

            supervisor.TenantId = tenantId.Value;

            _context.Add(supervisor);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Supervisor supervisor, byte[]? rowVersion = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var existing = await _context.Supervisors
                .FirstOrDefaultAsync(s => s.Id == supervisor.Id && (!tenantId.HasValue || s.TenantId == tenantId.Value));

            if (existing == null)
                throw new InvalidOperationException("Supervisor not found");

            existing.CompanyId = supervisor.CompanyId;
            existing.FirstName = supervisor.FirstName;
            existing.LastName = supervisor.LastName;
            existing.JobTitle = supervisor.JobTitle;
            existing.Email = supervisor.Email;
            existing.Phone = supervisor.Phone;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var supervisor = await _context.Supervisors
                .FirstOrDefaultAsync(s => s.Id == id && (!tenantId.HasValue || s.TenantId == tenantId.Value));

            if (supervisor != null)
            {
                _context.Supervisors.Remove(supervisor);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Supervisors
                .AnyAsync(s => s.Id == id && (!tenantId.HasValue || s.TenantId == tenantId.Value));
        }
    }
}

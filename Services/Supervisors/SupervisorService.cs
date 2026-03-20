using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Apiary.Data;
using Apiary.Models.School;
using Apiary.Models.Tables;

namespace Apiary.Services.Supervisors
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

        public async Task<PagedResult<Supervisor>> GetPagedAsync(TableQuery tableQuery, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.Supervisors
                .AsNoTracking()
                .Include(s => s.Company)
                .AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(s => s.TenantId == tenantId.Value);

            // Search
            if (!string.IsNullOrWhiteSpace(tableQuery.Search))
            {
                var search = tableQuery.Search.ToLower();
                query = query.Where(s =>
                    (s.FirstName + " " + s.LastName).ToLower().Contains(search) ||
                    (s.Company != null && s.Company.Name.ToLower().Contains(search)) ||
                    (s.Email != null && s.Email.ToLower().Contains(search)));
            }

            var totalCount = await query.CountAsync();

            // Sort
            query = tableQuery.Sort?.ToLower() switch
            {
                "company" => tableQuery.IsDescending
                    ? query.OrderByDescending(s => s.Company != null ? s.Company.Name : "")
                    : query.OrderBy(s => s.Company != null ? s.Company.Name : ""),
                "email" => tableQuery.IsDescending
                    ? query.OrderByDescending(s => s.Email)
                    : query.OrderBy(s => s.Email),
                _ => tableQuery.IsDescending
                    ? query.OrderByDescending(s => s.LastName).ThenByDescending(s => s.FirstName)
                    : query.OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            };

            var items = await query
                .Skip((tableQuery.Page - 1) * tableQuery.Size)
                .Take(tableQuery.Size)
                .ToListAsync();

            return new PagedResult<Supervisor>
            {
                Items = items,
                TotalCount = totalCount,
                Page = tableQuery.Page,
                Size = tableQuery.Size
            };
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

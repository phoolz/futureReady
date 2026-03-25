using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Apiary.Data;
using Apiary.Models.School;
using Apiary.Models.Tables;

namespace Apiary.Services.Companies
{
    public class CompanyService : ICompanyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public CompanyService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<List<Company>> GetAllAsync(Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.Companies.AsNoTracking().AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(c => c.TenantId == tenantId.Value);

            return await query.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<PagedResult<Company>> GetPagedAsync(TableQuery tableQuery, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.Companies.AsNoTracking().AsQueryable();

            if (tenantId.HasValue)
                query = query.Where(c => c.TenantId == tenantId.Value);

            // Search
            if (!string.IsNullOrWhiteSpace(tableQuery.Search))
            {
                var search = tableQuery.Search.ToLower();
                query = query.Where(c =>
                    c.Name.ToLower().Contains(search) ||
                    (c.Industry != null && c.Industry.ToLower().Contains(search)) ||
                    (c.City != null && c.City.ToLower().Contains(search)));
            }

            var totalCount = await query.CountAsync();

            // Sort
            query = tableQuery.Sort?.ToLower() switch
            {
                "industry" => tableQuery.IsDescending
                    ? query.OrderByDescending(c => c.Industry)
                    : query.OrderBy(c => c.Industry),
                "city" => tableQuery.IsDescending
                    ? query.OrderByDescending(c => c.City)
                    : query.OrderBy(c => c.City),
                _ => tableQuery.IsDescending
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name)
            };

            var items = await query
                .Skip((tableQuery.Page - 1) * tableQuery.Size)
                .Take(tableQuery.Size)
                .ToListAsync();

            return new PagedResult<Company>
            {
                Items = items,
                TotalCount = totalCount,
                Page = tableQuery.Page,
                Size = tableQuery.Size
            };
        }

        public async Task<Company?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Companies.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && (!tenantId.HasValue || c.TenantId == tenantId.Value));
        }

        public async Task CreateAsync(Company company, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating a company.");

            company.TenantId = tenantId.Value;

            _context.Add(company);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Company company, byte[]? rowVersion = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var existing = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == company.Id && (!tenantId.HasValue || c.TenantId == tenantId.Value));

            if (existing == null)
                throw new InvalidOperationException("Company not found");

            existing.Name = company.Name;
            existing.Industry = company.Industry;
            existing.StreetAddress = company.StreetAddress;
            existing.StreetAddress2 = company.StreetAddress2;
            existing.Suburb = company.Suburb;
            existing.City = company.City;
            existing.State = company.State;
            existing.PostalCode = company.PostalCode;
            existing.PostalStreetAddress = company.PostalStreetAddress;
            existing.PostalSuburb = company.PostalSuburb;
            existing.PostalCity = company.PostalCity;
            existing.PostalState = company.PostalState;
            existing.PostalPostalCode = company.PostalPostalCode;
            existing.PublicLiabilityInsurance5M = company.PublicLiabilityInsurance5M;
            existing.InsuranceValue = company.InsuranceValue;
            existing.HasPreviousWorkExperienceStudents = company.HasPreviousWorkExperienceStudents;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var company = await _context.Companies
                .FirstOrDefaultAsync(c => c.Id == id && (!tenantId.HasValue || c.TenantId == tenantId.Value));

            if (company != null)
            {
                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Companies
                .AnyAsync(c => c.Id == id && (!tenantId.HasValue || c.TenantId == tenantId.Value));
        }
    }
}

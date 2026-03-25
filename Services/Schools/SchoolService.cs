using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Apiary.Data;
using Apiary.Models.School;
using Apiary.Models.Tables;

namespace Apiary.Services.Schools
{
    public class SchoolService : ISchoolService
    {
        private readonly ApplicationDbContext _context;

        public SchoolService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<School>> GetAllAsync()
        {
            return await _context.Schools.AsNoTracking().ToListAsync();
        }

        public async Task<PagedResult<School>> GetPagedAsync(TableQuery tableQuery)
        {
            var query = _context.Schools.AsNoTracking().AsQueryable();

            // Search
            if (!string.IsNullOrWhiteSpace(tableQuery.Search))
            {
                var search = tableQuery.Search.ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(search) ||
                    s.TenantKey.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync();

            // Sort
            query = tableQuery.Sort?.ToLower() switch
            {
                "tenantkey" => tableQuery.IsDescending
                    ? query.OrderByDescending(s => s.TenantKey)
                    : query.OrderBy(s => s.TenantKey),
                _ => tableQuery.IsDescending
                    ? query.OrderByDescending(s => s.Name)
                    : query.OrderBy(s => s.Name)
            };

            var items = await query
                .Skip((tableQuery.Page - 1) * tableQuery.Size)
                .Take(tableQuery.Size)
                .ToListAsync();

            return new PagedResult<School>
            {
                Items = items,
                TotalCount = totalCount,
                Page = tableQuery.Page,
                Size = tableQuery.Size
            };
        }

        public async Task<School?> GetByIdAsync(Guid id)
        {
            return await _context.Schools.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task CreateAsync(School school)
        {
            _context.Add(school);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(School school, byte[]? rowVersion = null)
        {
            var existing = await _context.Schools.FirstOrDefaultAsync(s => s.Id == school.Id);
            if (existing == null) throw new InvalidOperationException("School not found");

            existing.Name = school.Name;
            existing.TenantKey = school.TenantKey;
            existing.Timezone = school.Timezone;
            existing.ContactEmail = school.ContactEmail;
            existing.ContactPhone = school.ContactPhone;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var school = await _context.Schools.FindAsync(id);
            if (school != null)
            {
                _context.Schools.Remove(school);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Schools.AnyAsync(s => s.Id == id);
        }
    }
}


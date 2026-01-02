using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;

namespace FutureReady.Services.Schools
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


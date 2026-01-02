using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;
using FutureReady.Services;

namespace FutureReady.Services.Students
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public StudentService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<List<Student>> GetAllAsync(Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.Set<Student>().Include(s => s.Cohort).AsNoTracking().AsQueryable();
            if (tenantId.HasValue) query = query.Where(s => s.TenantId == tenantId.Value);
            return await query.ToListAsync();
        }

        public async Task<Student?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Set<Student>().Include(s => s.Cohort).AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && (!tenantId.HasValue || s.TenantId == tenantId.Value));
        }

        public async Task CreateAsync(Student student, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue) throw new InvalidOperationException("Tenant must be known when creating a student.");

            student.TenantId = tenantId.Value;

            // Ensure cohort belongs to same tenant
            var cohort = await _context.Cohorts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == student.CohortId && c.TenantId == tenantId.Value);
            if (cohort == null) throw new InvalidOperationException("Invalid cohort for tenant");

            _context.Add(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Student student, byte[]? rowVersion = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var existing = await _context.Set<Student>().FirstOrDefaultAsync(s => s.Id == student.Id && (!tenantId.HasValue || s.TenantId == tenantId.Value));
            if (existing == null) throw new InvalidOperationException("Student not found");

            // prevent changing cohort to another tenant's cohort
            if (existing.CohortId != student.CohortId)
            {
                var cohort = await _context.Cohorts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == student.CohortId && (!tenantId.HasValue || c.TenantId == tenantId.Value));
                if (cohort == null) throw new InvalidOperationException("Invalid cohort for tenant");
                existing.CohortId = student.CohortId;
            }

            existing.MedicareNumber = student.MedicareNumber;
            existing.StudentType = student.StudentType;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var student = await _context.Set<Student>().FirstOrDefaultAsync(s => s.Id == id && (!tenantId.HasValue || s.TenantId == tenantId.Value));
            if (student != null)
            {
                _context.Set<Student>().Remove(student);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.Set<Student>().AnyAsync(s => s.Id == id && (!tenantId.HasValue || s.TenantId == tenantId.Value));
        }
    }
}


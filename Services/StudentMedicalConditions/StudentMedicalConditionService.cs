using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;

namespace FutureReady.Services.StudentMedicalConditions
{
    public class StudentMedicalConditionService : IStudentMedicalConditionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public StudentMedicalConditionService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<List<StudentMedicalCondition>> GetAllByStudentIdAsync(Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.StudentMedicalConditions
                .Include(e => e.Student)
                .AsNoTracking()
                .Where(e => e.StudentId == studentId);

            if (tenantId.HasValue)
                query = query.Where(e => e.TenantId == tenantId.Value);

            return await query.OrderBy(e => e.ConditionType).ToListAsync();
        }

        public async Task<StudentMedicalCondition?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.StudentMedicalConditions
                .Include(e => e.Student)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));
        }

        public async Task CreateAsync(StudentMedicalCondition condition, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating a medical condition.");

            var student = await _context.Students.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == condition.StudentId && s.TenantId == tenantId.Value);
            if (student == null)
                throw new InvalidOperationException("Invalid student for tenant");

            condition.TenantId = tenantId.Value;

            _context.Add(condition);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(StudentMedicalCondition condition, byte[]? rowVersion = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var existing = await _context.StudentMedicalConditions
                .FirstOrDefaultAsync(e => e.Id == condition.Id && (!tenantId.HasValue || e.TenantId == tenantId.Value));

            if (existing == null)
                throw new InvalidOperationException("Medical condition not found");

            existing.ConditionType = condition.ConditionType;
            existing.Details = condition.Details;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var condition = await _context.StudentMedicalConditions
                .FirstOrDefaultAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));

            if (condition != null)
            {
                _context.StudentMedicalConditions.Remove(condition);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.StudentMedicalConditions
                .AnyAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;

namespace FutureReady.Services.StudentWorkHistories
{
    public class StudentWorkHistoryService : IStudentWorkHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public StudentWorkHistoryService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<StudentWorkHistory?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.StudentWorkHistories
                .AsNoTracking()
                .Include(h => h.Student)
                .FirstOrDefaultAsync(h => h.Id == id && (!tenantId.HasValue || h.TenantId == tenantId.Value));
        }

        public async Task<StudentWorkHistory?> GetByStudentIdAsync(Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.StudentWorkHistories
                .AsNoTracking()
                .Include(h => h.Student)
                .FirstOrDefaultAsync(h => h.StudentId == studentId && (!tenantId.HasValue || h.TenantId == tenantId.Value));
        }

        public async Task CreateOrUpdateAsync(StudentWorkHistory history, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating or updating student work history.");

            var existing = await _context.StudentWorkHistories
                .FirstOrDefaultAsync(h => h.StudentId == history.StudentId && h.TenantId == tenantId.Value);

            if (existing != null)
            {
                existing.CurrentCourses = history.CurrentCourses;
                existing.VetQualifications = history.VetQualifications;
                existing.Certificates = history.Certificates;
                existing.PartTimeEmployment = history.PartTimeEmployment;
                existing.CommunityService = history.CommunityService;
            }
            else
            {
                history.TenantId = tenantId.Value;
                _context.StudentWorkHistories.Add(history);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var history = await _context.StudentWorkHistories
                .FirstOrDefaultAsync(h => h.Id == id && (!tenantId.HasValue || h.TenantId == tenantId.Value));

            if (history != null)
            {
                _context.StudentWorkHistories.Remove(history);
                await _context.SaveChangesAsync();
            }
        }
    }
}

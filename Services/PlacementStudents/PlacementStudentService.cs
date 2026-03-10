using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Apiary.Data;
using Apiary.Models.School;

namespace Apiary.Services.PlacementStudents
{
    public class PlacementStudentService : IPlacementStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public PlacementStudentService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<List<PlacementStudent>> GetByPlacementIdAsync(Guid placementId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.PlacementStudents
                .AsNoTracking()
                .Include(ps => ps.Student)
                .Where(ps => ps.PlacementId == placementId);

            if (tenantId.HasValue)
                query = query.Where(ps => ps.TenantId == tenantId.Value);

            return await query.OrderBy(ps => ps.Student!.LastName).ThenBy(ps => ps.Student!.FirstName).ToListAsync();
        }

        public async Task<PlacementStudent?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.PlacementStudents
                .AsNoTracking()
                .Include(ps => ps.Student)
                .Include(ps => ps.Placement)
                .FirstOrDefaultAsync(ps => ps.Id == id && (!tenantId.HasValue || ps.TenantId == tenantId.Value));
        }

        public async Task<PlacementStudent?> GetByPlacementAndStudentAsync(Guid placementId, Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.PlacementStudents
                .AsNoTracking()
                .Include(ps => ps.Student)
                .Include(ps => ps.Placement)
                .FirstOrDefaultAsync(ps => ps.PlacementId == placementId && ps.StudentId == studentId && (!tenantId.HasValue || ps.TenantId == tenantId.Value));
        }

        public async Task<List<PlacementStudent>> GetByStudentIdAsync(Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.PlacementStudents
                .AsNoTracking()
                .Include(ps => ps.Placement)
                    .ThenInclude(p => p!.Company)
                .Include(ps => ps.Placement)
                    .ThenInclude(p => p!.Supervisor)
                .Where(ps => ps.StudentId == studentId);

            if (tenantId.HasValue)
                query = query.Where(ps => ps.TenantId == tenantId.Value);

            return await query.OrderByDescending(ps => ps.Placement!.Year).ThenByDescending(ps => ps.CreatedAt).ToListAsync();
        }

        public async Task<PlacementStudent> AddStudentToPlacementAsync(Guid placementId, Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when adding a student to a placement.");

            // Check if student is already in this placement
            var existing = await _context.PlacementStudents
                .FirstOrDefaultAsync(ps => ps.PlacementId == placementId && ps.StudentId == studentId && ps.TenantId == tenantId.Value);

            if (existing != null)
                throw new InvalidOperationException("Student is already assigned to this placement.");

            var placementStudent = new PlacementStudent
            {
                PlacementId = placementId,
                StudentId = studentId,
                Status = "pending_parent",
                TenantId = tenantId.Value
            };

            _context.PlacementStudents.Add(placementStudent);
            await _context.SaveChangesAsync();

            // Load the student navigation property
            await _context.Entry(placementStudent).Reference(ps => ps.Student).LoadAsync();

            return placementStudent;
        }

        public async Task RemoveStudentFromPlacementAsync(Guid placementId, Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var placementStudent = await _context.PlacementStudents
                .FirstOrDefaultAsync(ps => ps.PlacementId == placementId && ps.StudentId == studentId && (!tenantId.HasValue || ps.TenantId == tenantId.Value));

            if (placementStudent != null)
            {
                _context.PlacementStudents.Remove(placementStudent);
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkParentSubmittedAsync(Guid placementId, Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var placementStudent = await _context.PlacementStudents
                .FirstOrDefaultAsync(ps => ps.PlacementId == placementId && ps.StudentId == studentId && (!tenantId.HasValue || ps.TenantId == tenantId.Value));

            if (placementStudent == null)
                throw new InvalidOperationException("PlacementStudent not found");

            placementStudent.Status = "confirmed";
            placementStudent.ParentSubmittedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(Guid placementStudentId, string status, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var placementStudent = await _context.PlacementStudents
                .FirstOrDefaultAsync(ps => ps.Id == placementStudentId && (!tenantId.HasValue || ps.TenantId == tenantId.Value));

            if (placementStudent == null)
                throw new InvalidOperationException("PlacementStudent not found");

            placementStudent.Status = status;

            await _context.SaveChangesAsync();
        }
    }
}

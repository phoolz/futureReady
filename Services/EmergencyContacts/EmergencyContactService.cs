using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;

namespace FutureReady.Services.EmergencyContacts
{
    public class EmergencyContactService : IEmergencyContactService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public EmergencyContactService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<List<EmergencyContact>> GetAllByStudentIdAsync(Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.EmergencyContacts
                .Include(e => e.Student)
                .AsNoTracking()
                .Where(e => e.StudentId == studentId);

            if (tenantId.HasValue)
                query = query.Where(e => e.TenantId == tenantId.Value);

            return await query.OrderByDescending(e => e.IsPrimary).ThenBy(e => e.LastName).ToListAsync();
        }

        public async Task<EmergencyContact?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.EmergencyContacts
                .Include(e => e.Student)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));
        }

        public async Task CreateAsync(EmergencyContact contact, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating an emergency contact.");

            // Verify student belongs to same tenant
            var student = await _context.Students.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == contact.StudentId && s.TenantId == tenantId.Value);
            if (student == null)
                throw new InvalidOperationException("Invalid student for tenant");

            contact.TenantId = tenantId.Value;

            _context.Add(contact);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EmergencyContact contact, byte[]? rowVersion = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var existing = await _context.EmergencyContacts
                .FirstOrDefaultAsync(e => e.Id == contact.Id && (!tenantId.HasValue || e.TenantId == tenantId.Value));

            if (existing == null)
                throw new InvalidOperationException("Emergency contact not found");

            existing.FirstName = contact.FirstName;
            existing.LastName = contact.LastName;
            existing.MobileNumber = contact.MobileNumber;
            existing.Relationship = contact.Relationship;
            existing.IsPrimary = contact.IsPrimary;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var contact = await _context.EmergencyContacts
                .FirstOrDefaultAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));

            if (contact != null)
            {
                _context.EmergencyContacts.Remove(contact);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.EmergencyContacts
                .AnyAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));
        }
    }
}

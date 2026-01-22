using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;
namespace FutureReady.Services.LogbookEntries
{
    public class LogbookEntryService : ILogbookEntryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public LogbookEntryService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<LogbookEntry?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.LogbookEntries
                .AsNoTracking()
                .Include(e => e.Placement)
                .FirstOrDefaultAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));
        }

        public async Task<List<LogbookEntry>> GetByPlacementIdAsync(Guid placementId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.LogbookEntries
                .AsNoTracking()
                .Include(e => e.Placement)
                .Where(e => e.PlacementId == placementId);

            if (tenantId.HasValue)
                query = query.Where(e => e.TenantId == tenantId.Value);

            return await query.OrderByDescending(e => e.Date).ToListAsync();
        }

        public async Task CreateAsync(LogbookEntry entry, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating a logbook entry.");

            entry.TenantId = tenantId.Value;

            _context.LogbookEntries.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LogbookEntry entry, byte[]? rowVersion = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var existing = await _context.LogbookEntries
                .FirstOrDefaultAsync(e => e.Id == entry.Id && (!tenantId.HasValue || e.TenantId == tenantId.Value));

            if (existing == null)
                throw new InvalidOperationException("Logbook entry not found");

            existing.PlacementId = entry.PlacementId;
            existing.Date = entry.Date;
            existing.StartTime = entry.StartTime;
            existing.LunchStartTime = entry.LunchStartTime;
            existing.LunchEndTime = entry.LunchEndTime;
            existing.FinishTime = entry.FinishTime;
            existing.TotalHoursWorked = entry.TotalHoursWorked;
            existing.CumulativeHours = entry.CumulativeHours;
            existing.SupervisorVerified = entry.SupervisorVerified;
            existing.SupervisorVerifiedAt = entry.SupervisorVerifiedAt;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var entry = await _context.LogbookEntries
                .FirstOrDefaultAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));

            if (entry != null)
            {
                _context.LogbookEntries.Remove(entry);
                await _context.SaveChangesAsync();
            }
        }

        public async Task VerifyAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var entry = await _context.LogbookEntries
                .FirstOrDefaultAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));

            if (entry == null)
                throw new InvalidOperationException("Logbook entry not found");

            entry.SupervisorVerified = true;
            entry.SupervisorVerifiedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<decimal> GetTotalHoursAsync(Guid placementId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.LogbookEntries
                .Where(e => e.PlacementId == placementId);

            if (tenantId.HasValue)
                query = query.Where(e => e.TenantId == tenantId.Value);

            return await query.SumAsync(e => e.TotalHoursWorked);
        }
    }
}

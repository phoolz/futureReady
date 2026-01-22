using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;
namespace FutureReady.Services.LogbookTasks
{
    public class LogbookTaskService : ILogbookTaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public LogbookTaskService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<LogbookTask?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.LogbookTasks
                .AsNoTracking()
                .Include(t => t.Placement)
                .FirstOrDefaultAsync(t => t.Id == id && (!tenantId.HasValue || t.TenantId == tenantId.Value));
        }

        public async Task<List<LogbookTask>> GetByPlacementIdAsync(Guid placementId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.LogbookTasks
                .AsNoTracking()
                .Include(t => t.Placement)
                .Where(t => t.PlacementId == placementId);

            if (tenantId.HasValue)
                query = query.Where(t => t.TenantId == tenantId.Value);

            return await query.OrderByDescending(t => t.DatePerformed).ToListAsync();
        }

        public async Task<List<LogbookTask>> GetByDateAsync(Guid placementId, DateOnly date, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.LogbookTasks
                .AsNoTracking()
                .Include(t => t.Placement)
                .Where(t => t.PlacementId == placementId && t.DatePerformed == date);

            if (tenantId.HasValue)
                query = query.Where(t => t.TenantId == tenantId.Value);

            return await query.OrderBy(t => t.CreatedAt).ToListAsync();
        }

        public async Task CreateAsync(LogbookTask task, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating a logbook task.");

            task.TenantId = tenantId.Value;

            _context.LogbookTasks.Add(task);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LogbookTask task, byte[]? rowVersion = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var existing = await _context.LogbookTasks
                .FirstOrDefaultAsync(t => t.Id == task.Id && (!tenantId.HasValue || t.TenantId == tenantId.Value));

            if (existing == null)
                throw new InvalidOperationException("Logbook task not found");

            existing.PlacementId = task.PlacementId;
            existing.Description = task.Description;
            existing.DatePerformed = task.DatePerformed;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var task = await _context.LogbookTasks
                .FirstOrDefaultAsync(t => t.Id == id && (!tenantId.HasValue || t.TenantId == tenantId.Value));

            if (task != null)
            {
                _context.LogbookTasks.Remove(task);
                await _context.SaveChangesAsync();
            }
        }
    }
}

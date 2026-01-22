using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;

namespace FutureReady.Services.LogbookEvaluations
{
    public class LogbookEvaluationService : ILogbookEvaluationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;

        public LogbookEvaluationService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<LogbookEvaluation?> GetByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.LogbookEvaluations
                .AsNoTracking()
                .Include(e => e.Placement)
                .FirstOrDefaultAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));
        }

        public async Task<List<LogbookEvaluation>> GetByPlacementIdAsync(Guid placementId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var query = _context.LogbookEvaluations
                .AsNoTracking()
                .Include(e => e.Placement)
                .Where(e => e.PlacementId == placementId);

            if (tenantId.HasValue)
                query = query.Where(e => e.TenantId == tenantId.Value);

            return await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
        }

        public async Task CreateAsync(LogbookEvaluation evaluation, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating a logbook evaluation.");

            evaluation.TenantId = tenantId.Value;

            _context.LogbookEvaluations.Add(evaluation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(LogbookEvaluation evaluation, byte[]? rowVersion = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var existing = await _context.LogbookEvaluations
                .FirstOrDefaultAsync(e => e.Id == evaluation.Id && (!tenantId.HasValue || e.TenantId == tenantId.Value));

            if (existing == null)
                throw new InvalidOperationException("Logbook evaluation not found");

            existing.PlacementId = evaluation.PlacementId;
            existing.AttendancePunctuality = evaluation.AttendancePunctuality;
            existing.Appearance = evaluation.Appearance;
            existing.CommunicationSkills = evaluation.CommunicationSkills;
            existing.Initiative = evaluation.Initiative;
            existing.WorkQuality = evaluation.WorkQuality;
            existing.Teamwork = evaluation.Teamwork;
            existing.SafetyAwareness = evaluation.SafetyAwareness;
            existing.OverallPerformance = evaluation.OverallPerformance;
            existing.SupervisorName = evaluation.SupervisorName;
            existing.Comments = evaluation.Comments;
            existing.SupervisorSignedAt = evaluation.SupervisorSignedAt;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var evaluation = await _context.LogbookEvaluations
                .FirstOrDefaultAsync(e => e.Id == id && (!tenantId.HasValue || e.TenantId == tenantId.Value));

            if (evaluation != null)
            {
                _context.LogbookEvaluations.Remove(evaluation);
                await _context.SaveChangesAsync();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FutureReady.Models.School;

namespace FutureReady.Services.Interfaces
{
    public interface ILogbookEntryService
    {
        Task<LogbookEntry?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task<List<LogbookEntry>> GetByPlacementIdAsync(Guid placementId, Guid? tenantId = null);
        Task CreateAsync(LogbookEntry entry, Guid? tenantId = null);
        Task UpdateAsync(LogbookEntry entry, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
        Task VerifyAsync(Guid id, Guid? tenantId = null);
        Task<decimal> GetTotalHoursAsync(Guid placementId, Guid? tenantId = null);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FutureReady.Models.School;

namespace FutureReady.Services.LogbookTasks
{
    public interface ILogbookTaskService
    {
        Task<LogbookTask?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task<List<LogbookTask>> GetByPlacementIdAsync(Guid placementId, Guid? tenantId = null);
        Task<List<LogbookTask>> GetByDateAsync(Guid placementId, DateOnly date, Guid? tenantId = null);
        Task CreateAsync(LogbookTask task, Guid? tenantId = null);
        Task UpdateAsync(LogbookTask task, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
    }
}

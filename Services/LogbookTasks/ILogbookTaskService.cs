using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apiary.Models.School;

namespace Apiary.Services.LogbookTasks
{
    public interface ILogbookTaskService
    {
        Task<LogbookTask?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task<List<LogbookTask>> GetByPlacementStudentIdAsync(Guid placementStudentId, Guid? tenantId = null);
        Task<List<LogbookTask>> GetByDateAsync(Guid placementStudentId, DateOnly date, Guid? tenantId = null);
        Task CreateAsync(LogbookTask task, Guid? tenantId = null);
        Task UpdateAsync(LogbookTask task, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
    }
}

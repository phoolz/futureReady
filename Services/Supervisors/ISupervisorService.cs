using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FutureReady.Models.School;

namespace FutureReady.Services.Supervisors
{
    public interface ISupervisorService
    {
        Task<List<Supervisor>> GetAllAsync(Guid? tenantId = null);
        Task<List<Supervisor>> GetByCompanyAsync(Guid companyId, Guid? tenantId = null);
        Task<Supervisor?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task CreateAsync(Supervisor supervisor, Guid? tenantId = null);
        Task UpdateAsync(Supervisor supervisor, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
        Task<bool> ExistsAsync(Guid id, Guid? tenantId = null);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FutureReady.Models.School;

namespace FutureReady.Services.Cohorts
{
    public interface ICohortService
    {
        Task<List<Cohort>> GetAllAsync(Guid? tenantId = null);
        Task<Cohort?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task CreateAsync(Cohort cohort, Guid? tenantId = null);
        Task UpdateAsync(Cohort cohort, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
        Task<bool> ExistsAsync(Guid id, Guid? tenantId = null);
    }
}


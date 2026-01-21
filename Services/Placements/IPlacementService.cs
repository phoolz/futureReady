using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FutureReady.Models.School;

namespace FutureReady.Services.Placements
{
    public interface IPlacementService
    {
        Task<List<Placement>> GetAllAsync(Guid? tenantId = null);
        Task<List<Placement>> GetByStudentIdAsync(Guid studentId, Guid? tenantId = null);
        Task<List<Placement>> GetByCompanyIdAsync(Guid companyId, Guid? tenantId = null);
        Task<Placement?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task<Placement?> GetByIdWithDetailsAsync(Guid id, Guid? tenantId = null);
        Task CreateAsync(Placement placement, Guid? tenantId = null);
        Task UpdateAsync(Placement placement, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
        Task<bool> ExistsAsync(Guid id, Guid? tenantId = null);
    }
}

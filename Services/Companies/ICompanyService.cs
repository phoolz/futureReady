using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FutureReady.Models.School;

namespace FutureReady.Services.Companies
{
    public interface ICompanyService
    {
        Task<List<Company>> GetAllAsync(Guid? tenantId = null);
        Task<Company?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task CreateAsync(Company company, Guid? tenantId = null);
        Task UpdateAsync(Company company, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
        Task<bool> ExistsAsync(Guid id, Guid? tenantId = null);
    }
}

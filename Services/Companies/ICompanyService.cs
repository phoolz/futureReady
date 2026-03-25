using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apiary.Models.School;
using Apiary.Models.Tables;

namespace Apiary.Services.Companies
{
    public interface ICompanyService
    {
        Task<List<Company>> GetAllAsync(Guid? tenantId = null);
        Task<PagedResult<Company>> GetPagedAsync(TableQuery query, Guid? tenantId = null);
        Task<Company?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task CreateAsync(Company company, Guid? tenantId = null);
        Task UpdateAsync(Company company, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
        Task<bool> ExistsAsync(Guid id, Guid? tenantId = null);
    }
}

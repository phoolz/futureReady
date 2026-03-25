using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apiary.Models.School;
using Apiary.Models.Tables;

namespace Apiary.Services.Students
{
    public interface IStudentService
    {
        Task<List<Student>> GetAllAsync(Guid? tenantId = null);
        Task<PagedResult<Student>> GetPagedAsync(TableQuery query, Guid? tenantId = null);
        Task<Student?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task CreateAsync(Student student, Guid? tenantId = null);
        Task UpdateAsync(Student student, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
        Task<bool> ExistsAsync(Guid id, Guid? tenantId = null);
        Task<Student?> GetByUserIdAsync(Guid userId, Guid? tenantId = null);
    }
}


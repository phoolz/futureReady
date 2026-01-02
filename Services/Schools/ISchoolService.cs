using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FutureReady.Models.School;

namespace FutureReady.Services.Schools
{
    public interface ISchoolService
    {
        Task<List<School>> GetAllAsync();
        Task<School?> GetByIdAsync(Guid id);
        Task CreateAsync(School school);
        Task UpdateAsync(School school, byte[]? rowVersion = null);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
}


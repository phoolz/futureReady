using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FutureReady.Models.School;

namespace FutureReady.Services.StudentMedicalConditions
{
    public interface IStudentMedicalConditionService
    {
        Task<List<StudentMedicalCondition>> GetAllByStudentIdAsync(Guid studentId, Guid? tenantId = null);
        Task<StudentMedicalCondition?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task CreateAsync(StudentMedicalCondition condition, Guid? tenantId = null);
        Task UpdateAsync(StudentMedicalCondition condition, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
        Task<bool> ExistsAsync(Guid id, Guid? tenantId = null);
    }
}

using System;
using System.Threading.Tasks;
using FutureReady.Models.School;

namespace FutureReady.Services.StudentWorkHistories
{
    public interface IStudentWorkHistoryService
    {
        Task<StudentWorkHistory?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task<StudentWorkHistory?> GetByStudentIdAsync(Guid studentId, Guid? tenantId = null);
        Task CreateOrUpdateAsync(StudentWorkHistory history, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
    }
}

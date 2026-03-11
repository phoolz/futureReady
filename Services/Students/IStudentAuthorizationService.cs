using System;
using System.Threading.Tasks;

namespace Apiary.Services.Students
{
    public interface IStudentAuthorizationService
    {
        Task<bool> CanAccessStudentDataAsync(Guid studentId);
        Task<Guid?> GetCurrentUserStudentIdAsync();
    }
}

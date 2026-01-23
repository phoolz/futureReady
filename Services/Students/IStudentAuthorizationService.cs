using System;
using System.Threading.Tasks;

namespace FutureReady.Services.Students
{
    public interface IStudentAuthorizationService
    {
        Task<bool> CanAccessStudentDataAsync(Guid studentId);
        Task<Guid?> GetCurrentUserStudentIdAsync();
    }
}

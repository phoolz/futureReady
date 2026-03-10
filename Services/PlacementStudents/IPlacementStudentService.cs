using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apiary.Models.School;

namespace Apiary.Services.PlacementStudents
{
    public interface IPlacementStudentService
    {
        Task<List<PlacementStudent>> GetByPlacementIdAsync(Guid placementId, Guid? tenantId = null);
        Task<PlacementStudent?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task<PlacementStudent?> GetByPlacementAndStudentAsync(Guid placementId, Guid studentId, Guid? tenantId = null);
        Task<List<PlacementStudent>> GetByStudentIdAsync(Guid studentId, Guid? tenantId = null);
        Task<PlacementStudent> AddStudentToPlacementAsync(Guid placementId, Guid studentId, Guid? tenantId = null);
        Task RemoveStudentFromPlacementAsync(Guid placementId, Guid studentId, Guid? tenantId = null);
        Task MarkParentSubmittedAsync(Guid placementId, Guid studentId, Guid? tenantId = null);
        Task UpdateStatusAsync(Guid placementStudentId, string status, Guid? tenantId = null);
    }
}

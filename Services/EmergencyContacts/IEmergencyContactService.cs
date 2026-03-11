using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apiary.Models.School;

namespace Apiary.Services.EmergencyContacts
{
    public interface IEmergencyContactService
    {
        Task<List<EmergencyContact>> GetAllByStudentIdAsync(Guid studentId, Guid? tenantId = null);
        Task<EmergencyContact?> GetByIdAsync(Guid id, Guid? tenantId = null);
        Task CreateAsync(EmergencyContact contact, Guid? tenantId = null);
        Task UpdateAsync(EmergencyContact contact, byte[]? rowVersion = null, Guid? tenantId = null);
        Task DeleteAsync(Guid id, Guid? tenantId = null);
        Task<bool> ExistsAsync(Guid id, Guid? tenantId = null);
    }
}

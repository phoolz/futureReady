using System.Threading.Tasks;
using Apiary.Models.ParentForm;

namespace Apiary.Services.ParentForm
{
    public interface IParentFormService
    {
        Task<ParentFormDto?> InitializeFormAsync(string token);
        Task<bool> SubmitFormAsync(string token, ParentFormDto formData);

        // Per-step save methods for Static SSR PRG pattern
        Task<bool> SaveStudentDetailsAsync(string token, StudentDetailsDto data);
        Task<bool> SaveEmergencyContactAsync(string token, EmergencyContactDto data);
        Task<bool> SaveWorkplaceDetailsAsync(string token, WorkplaceDetailsDto data);
        Task<bool> SaveTransportAsync(string token, TransportDto data);
        Task<bool> SaveMedicalDetailsAsync(string token, MedicalDetailsDto data);
        Task<bool> SaveConsentAsync(string token, ConsentDto data);
        Task<bool> FinalizeSubmissionAsync(string token);
    }
}

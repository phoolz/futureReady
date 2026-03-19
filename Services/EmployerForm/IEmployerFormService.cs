using System;
using System.Threading.Tasks;
using Apiary.Models.EmployerForm;

namespace Apiary.Services.EmployerForm
{
    public interface IEmployerFormService
    {
        Task<EmployerFormDto?> InitializeFormAsync(string token);
        Task<bool> SubmitFormAsync(string token, EmployerFormDto formData);

        // Per-step save methods for Static SSR PRG pattern
        Task<bool> SaveWorkplaceDetailsAsync(string token, WorkplaceDetailsDto data);
        Task<bool> SaveSupervisorDetailsAsync(string token, SupervisorDetailsDto data);
        Task<bool> SaveInsuranceAsync(string token, InsuranceDto data);
        Task<bool> SaveOhsAsync(string token, OhsDto data);
        Task<bool> SaveGeneralTravelAsync(string token, GeneralTravelDto data);
        Task<bool> SaveHazardsAppendixAsync(string token, HazardsAppendixDto data);
        Task<bool> FinalizeSubmissionAsync(string token);
    }
}

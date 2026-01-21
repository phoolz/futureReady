using System;
using System.Threading.Tasks;
using FutureReady.Models.EmployerForm;

namespace FutureReady.Services.EmployerForm
{
    public interface IEmployerFormService
    {
        Task<EmployerFormDto?> InitializeFormAsync(string token);
        Task<bool> SubmitFormAsync(string token, EmployerFormDto formData);
    }
}

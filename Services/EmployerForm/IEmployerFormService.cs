using System;
using System.Threading.Tasks;
using Apiary.Models.EmployerForm;

namespace Apiary.Services.EmployerForm
{
    public interface IEmployerFormService
    {
        Task<EmployerFormDto?> InitializeFormAsync(string token);
        Task<bool> SubmitFormAsync(string token, EmployerFormDto formData);
    }
}

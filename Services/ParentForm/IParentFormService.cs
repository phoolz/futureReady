using System.Threading.Tasks;
using FutureReady.Models.ParentForm;

namespace FutureReady.Services.ParentForm
{
    public interface IParentFormService
    {
        Task<ParentFormDto?> InitializeFormAsync(string token);
        Task<bool> SubmitFormAsync(string token, ParentFormDto formData);
    }
}

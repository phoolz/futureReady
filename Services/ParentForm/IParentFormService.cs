using System.Threading.Tasks;
using Apiary.Models.ParentForm;

namespace Apiary.Services.ParentForm
{
    public interface IParentFormService
    {
        Task<ParentFormDto?> InitializeFormAsync(string token);
        Task<bool> SubmitFormAsync(string token, ParentFormDto formData);
    }
}

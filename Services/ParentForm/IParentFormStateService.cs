using System.Threading.Tasks;
using FutureReady.Models.ParentForm;

namespace FutureReady.Services.ParentForm
{
    public interface IParentFormStateService
    {
        ParentFormDto FormData { get; }
        string Token { get; }
        int CurrentStep { get; }
        bool IsInitialized { get; }
        bool IsLoading { get; }
        string? ErrorMessage { get; }

        Task InitializeAsync(string token);
        Task SaveToStorageAsync();
        Task LoadFromStorageAsync(string token);
        Task ClearStorageAsync(string token);
        void SetCurrentStep(int step);
        bool ValidateCurrentStep();
        bool CanNavigateToStep(int step);
    }
}

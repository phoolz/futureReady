using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using FutureReady.Models.ParentForm;

namespace FutureReady.Services.ParentForm
{
    public class ParentFormStateService : IParentFormStateService
    {
        private readonly IParentFormService _formService;
        private readonly IJSRuntime _jsRuntime;

        public ParentFormDto FormData { get; private set; } = new();
        public string Token { get; private set; } = string.Empty;
        public int CurrentStep { get; private set; } = 1;
        public bool IsInitialized { get; private set; }
        public bool IsLoading { get; private set; }
        public string? ErrorMessage { get; private set; }

        private const int TotalSteps = 7;

        public ParentFormStateService(IParentFormService formService, IJSRuntime jsRuntime)
        {
            _formService = formService;
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync(string token)
        {
            if (IsInitialized && Token == token)
            {
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            Token = token;

            try
            {
                // Try to load from localStorage first
                await LoadFromStorageAsync(token);

                // If no localStorage data, initialize from database
                if (FormData.PlacementId == Guid.Empty)
                {
                    var dbData = await _formService.InitializeFormAsync(token);
                    if (dbData == null)
                    {
                        ErrorMessage = "Invalid or expired form link.";
                        IsLoading = false;
                        return;
                    }
                    FormData = dbData;
                }

                CurrentStep = FormData.CurrentStep > 0 ? FormData.CurrentStep : 1;
                IsInitialized = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred loading the form.";
                Console.WriteLine($"Error initializing form: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task SaveToStorageAsync()
        {
            try
            {
                FormData.CurrentStep = CurrentStep;
                FormData.LastSavedAt = DateTime.UtcNow;
                var json = JsonSerializer.Serialize(FormData);
                await _jsRuntime.InvokeVoidAsync("parentFormStorage.save", Token, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving to localStorage: {ex.Message}");
            }
        }

        public async Task LoadFromStorageAsync(string token)
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string?>("parentFormStorage.load", token);
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonSerializer.Deserialize<ParentFormDto>(json);
                    if (data != null)
                    {
                        FormData = data;
                        CurrentStep = data.CurrentStep > 0 ? data.CurrentStep : 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading from localStorage: {ex.Message}");
            }
        }

        public async Task ClearStorageAsync(string token)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("parentFormStorage.clear", token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing localStorage: {ex.Message}");
            }
        }

        public void SetCurrentStep(int step)
        {
            if (step >= 1 && step <= TotalSteps)
            {
                CurrentStep = step;
                FormData.CurrentStep = step;
            }
        }

        public bool ValidateCurrentStep()
        {
            return CurrentStep switch
            {
                1 => ValidateStudentDetails(),
                2 => ValidateEmergencyContact(),
                3 => ValidateWorkplaceDetails(),
                4 => ValidateTransport(),
                5 => true, // Medical details has no required fields
                6 => ValidateConsent(),
                7 => ValidateAllSteps(),
                _ => false
            };
        }

        public bool CanNavigateToStep(int step)
        {
            // Can always go back
            if (step < CurrentStep)
            {
                return true;
            }

            // To go forward, validate all steps up to the target
            for (int i = CurrentStep; i < step; i++)
            {
                var tempStep = CurrentStep;
                CurrentStep = i;
                if (!ValidateCurrentStep())
                {
                    CurrentStep = tempStep;
                    return false;
                }
                CurrentStep = tempStep;
            }

            return true;
        }

        private bool ValidateStudentDetails()
        {
            var sd = FormData.StudentDetails;
            return !string.IsNullOrWhiteSpace(sd.StudentType)
                && !string.IsNullOrWhiteSpace(sd.MobileNumber);
        }

        private bool ValidateEmergencyContact()
        {
            var ec = FormData.EmergencyContact;
            return !string.IsNullOrWhiteSpace(ec.FirstName)
                && !string.IsNullOrWhiteSpace(ec.LastName)
                && !string.IsNullOrWhiteSpace(ec.MobileNumber)
                && !string.IsNullOrWhiteSpace(ec.Relationship);
        }

        private bool ValidateWorkplaceDetails()
        {
            var wd = FormData.WorkplaceDetails;

            // If company is pre-set, no validation needed (read-only)
            if (wd.IsCompanyPreset)
            {
                return true;
            }

            // Otherwise validate all fields
            return !string.IsNullOrWhiteSpace(wd.CompanyName)
                && !string.IsNullOrWhiteSpace(wd.ContactFirstName)
                && !string.IsNullOrWhiteSpace(wd.ContactLastName)
                && !string.IsNullOrWhiteSpace(wd.ContactEmail)
                && !string.IsNullOrWhiteSpace(wd.ContactPhone)
                && !string.IsNullOrWhiteSpace(wd.StreetAddress)
                && !string.IsNullOrWhiteSpace(wd.City)
                && !string.IsNullOrWhiteSpace(wd.State)
                && !string.IsNullOrWhiteSpace(wd.PostalCode);
        }

        private bool ValidateTransport()
        {
            var t = FormData.Transport;

            if (string.IsNullOrWhiteSpace(t.TransportMethod))
            {
                return false;
            }

            // If private_car or combination, require driver details
            if (t.TransportMethod == "private_car" || t.TransportMethod == "combination")
            {
                if (string.IsNullOrWhiteSpace(t.DriverName) || string.IsNullOrWhiteSpace(t.DriverContactNumber))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ValidateConsent()
        {
            var c = FormData.Consent;
            return !string.IsNullOrWhiteSpace(c.ParentFirstName)
                && !string.IsNullOrWhiteSpace(c.ParentLastName)
                && c.ConsentGiven;
        }

        private bool ValidateAllSteps()
        {
            return ValidateStudentDetails()
                && ValidateEmergencyContact()
                && ValidateWorkplaceDetails()
                && ValidateTransport()
                && ValidateConsent();
        }
    }
}

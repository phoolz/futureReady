using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using FutureReady.Models.EmployerForm;

namespace FutureReady.Services.EmployerForm
{
    public class EmployerFormStateService : IEmployerFormStateService
    {
        private readonly IEmployerFormService _formService;
        private readonly IJSRuntime _jsRuntime;

        public EmployerFormDto FormData { get; private set; } = new();
        public string Token { get; private set; } = string.Empty;
        public int CurrentStep { get; private set; } = 1;
        public bool IsInitialized { get; private set; }
        public bool IsLoading { get; private set; }
        public string? ErrorMessage { get; private set; }

        private const int TotalSteps = 7;

        public EmployerFormStateService(IEmployerFormService formService, IJSRuntime jsRuntime)
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
                await _jsRuntime.InvokeVoidAsync("employerFormStorage.save", Token, json);
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
                var json = await _jsRuntime.InvokeAsync<string?>("employerFormStorage.load", token);
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonSerializer.Deserialize<EmployerFormDto>(json);
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
                await _jsRuntime.InvokeVoidAsync("employerFormStorage.clear", token);
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
                1 => ValidateWorkplaceDetails(),
                2 => ValidateSupervisorDetails(),
                3 => ValidateInsurance(),
                4 => ValidateOhs(),
                5 => ValidateGeneralTravel(),
                6 => true, // Hazards appendix has no required fields
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

        private bool ValidateWorkplaceDetails()
        {
            var wd = FormData.WorkplaceDetails;
            return !string.IsNullOrWhiteSpace(wd.StreetAddress)
                && !string.IsNullOrWhiteSpace(wd.Suburb)
                && !string.IsNullOrWhiteSpace(wd.City)
                && !string.IsNullOrWhiteSpace(wd.State)
                && !string.IsNullOrWhiteSpace(wd.PostalCode)
                && !string.IsNullOrWhiteSpace(wd.WorkStartTime)
                && !string.IsNullOrWhiteSpace(wd.WorkEndTime);
        }

        private bool ValidateSupervisorDetails()
        {
            var sd = FormData.SupervisorDetails;
            return !string.IsNullOrWhiteSpace(sd.FirstName)
                && !string.IsNullOrWhiteSpace(sd.LastName)
                && !string.IsNullOrWhiteSpace(sd.Email)
                && !string.IsNullOrWhiteSpace(sd.Phone);
        }

        private bool ValidateInsurance()
        {
            var ins = FormData.Insurance;
            return ins.HasPublicLiabilityInsurance5M.HasValue
                && ins.HasPreviousWorkExperienceStudents.HasValue;
        }

        private bool ValidateOhs()
        {
            var ohs = FormData.Ohs;
            return ohs.HasOhsPolicy.HasValue
                && ohs.HasInductionProgram.HasValue
                && ohs.HasObviousHazards.HasValue
                && ohs.ProvidesHazardReportingInstruction.HasValue
                && ohs.HasEmergencyProcedures.HasValue
                && ohs.HasFireExtinguishersChecked.HasValue
                && ohs.HasFirstAidKit.HasValue
                && ohs.HasSafeAmenities.HasValue;
        }

        private bool ValidateGeneralTravel()
        {
            var gt = FormData.GeneralTravel;
            return gt.StaffInformedOfStudent.HasValue
                && gt.StaffMeetWorkingWithChildrenRequirements.HasValue
                && gt.AdditionalInfoRequired.HasValue
                && gt.RequiresVehicleTravel.HasValue;
        }

        private bool ValidateAllSteps()
        {
            return ValidateWorkplaceDetails()
                && ValidateSupervisorDetails()
                && ValidateInsurance()
                && ValidateOhs()
                && ValidateGeneralTravel();
        }
    }
}

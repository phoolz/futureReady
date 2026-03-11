using System.Threading.Tasks;
using Apiary.Models.StudentActivation;

namespace Apiary.Services.StudentActivation
{
    public interface IStudentActivationService
    {
        /// <summary>
        /// Initializes the activation form with student information from the token.
        /// Returns null if token is invalid.
        /// </summary>
        Task<StudentActivationDto?> InitializeFormAsync(string token);

        /// <summary>
        /// Activates the student account: creates ApplicationUser, links to Student, updates profile.
        /// Returns (success, errorMessage).
        /// </summary>
        Task<(bool Success, string? ErrorMessage)> ActivateAccountAsync(string token, StudentActivationDto dto);
    }
}

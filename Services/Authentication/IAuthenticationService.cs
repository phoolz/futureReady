using System.Threading.Tasks;

namespace FutureReady.Services.Authentication
{
    public interface IAuthenticationService
    {
        /// <summary>
        /// Validates credentials and, on success, signs the user in (issues cookie) via the current HttpContext.
        /// Returns (true, null) on success or (false, errorMessage) on failure.
        /// </summary>
        Task<(bool Success, string? Error)> SignInAsync(string username, string password, bool rememberMe);

        /// <summary>
        /// Signs the current HTTP context out of the authentication scheme.
        /// </summary>
        Task SignOutAsync();
    }
}


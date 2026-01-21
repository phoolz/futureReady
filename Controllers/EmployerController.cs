using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FutureReady.Controllers
{
    [AllowAnonymous]
    public class EmployerController : Controller
    {
        // Error views for token validation
        // These are used by both the Blazor component and can be accessed directly

        [HttpGet("employer/token-invalid")]
        public IActionResult TokenInvalid()
        {
            return View("TokenInvalid");
        }

        [HttpGet("employer/token-expired")]
        public IActionResult TokenExpired()
        {
            return View("TokenExpired");
        }

        [HttpGet("employer/form-submitted")]
        public IActionResult FormSubmitted()
        {
            return View("FormSubmitted");
        }
    }
}

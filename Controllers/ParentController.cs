using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace FutureReady.Controllers
{
    [AllowAnonymous]
    public class ParentController : Controller
    {
        [HttpGet("parent/token-invalid")]
        public IActionResult TokenInvalid()
        {
            return View("TokenInvalid");
        }

        [HttpGet("parent/token-expired")]
        public IActionResult TokenExpired()
        {
            return View("TokenExpired");
        }

        [HttpGet("parent/form-submitted")]
        public IActionResult FormSubmitted()
        {
            return View("FormSubmitted");
        }
    }
}

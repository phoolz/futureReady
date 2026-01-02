using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Services;
// avoid ambiguous IAuthenticationService name (Microsoft.AspNetCore.Authentication.IAuthenticationService)
// use fully-qualified name in the controller fields/ctor
using FutureReady.Services.Authentication;

namespace FutureReady.Controllers
{
    public class AuthenticationController : Controller
    {
        private readonly ApplicationDbContext _context; // keep for view needs if any
        private readonly FutureReady.Services.Authentication.IAuthenticationService _authService;

        public AuthenticationController(ApplicationDbContext context, FutureReady.Services.Authentication.IAuthenticationService authService)
        {
            _context = context;
            _authService = authService;
        }

        // GET: /Authentication/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Authentication/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var (success, error) = await _authService.SignInAsync(model.Username ?? string.Empty, model.Password ?? string.Empty, model.RememberMe);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Invalid username or password");
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        // POST: /Authentication/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _authService.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }

    public class LoginViewModel
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }
}

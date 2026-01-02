using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models;

namespace FutureReady.Services.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<(bool Success, string? Error)> SignInAsync(string username, string password, bool rememberMe)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                return (false, "Invalid username or password");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username || u.Email == username);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return (false, "Invalid username or password");

            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
                return (false, "Invalid username or password");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? "user"),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("DisplayName", user.DisplayName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe
            };

            var httpContext = _httpContextAccessor.HttpContext
                              ?? throw new InvalidOperationException("No HttpContext available for authentication");

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity), authProperties);

            return (true, null);
        }

        public async Task SignOutAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext
                              ?? throw new InvalidOperationException("No HttpContext available for sign-out");

            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}


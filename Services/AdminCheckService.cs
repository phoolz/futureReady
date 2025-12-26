using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Services;

namespace FutureReady.Services
{
    public interface IAdminCheckService
    {
        Task<bool> IsCurrentUserAdminAsync();
        Task<bool> IsUserAdminAsync(ClaimsPrincipal principal);
    }

    public class AdminCheckService : IAdminCheckService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserProvider? _userProvider;
        private readonly ApplicationDbContext _db;

        public AdminCheckService(IHttpContextAccessor httpContextAccessor, IUserProvider? userProvider, ApplicationDbContext db)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userProvider = userProvider;
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Task<bool> IsCurrentUserAdminAsync()
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            return IsUserAdminAsync(principal);
        }

        public async Task<bool> IsUserAdminAsync(ClaimsPrincipal principal)
        {
            if (principal == null) return false;

            // Try IUserProvider first (may return username or email)
            var identifier = _userProvider?.GetCurrentUsername();

            // Fallback to principal name or email claim
            if (string.IsNullOrWhiteSpace(identifier))
            {
                identifier = principal.Identity?.Name;
            }
            if (string.IsNullOrWhiteSpace(identifier))
            {
                identifier = principal.FindFirst(ClaimTypes.Email)?.Value;
            }

            if (string.IsNullOrWhiteSpace(identifier)) return false;

            // Find the user by username or email (case-insensitive)
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == identifier.ToLower() || u.Email.ToLower() == identifier.ToLower());
            if (user == null) return false;

            // Check if there's a school with Id == user.TenantId and Name == "Admin" (case-insensitive)
            var isAdmin = await _db.Schools.AnyAsync(s => s.Id == user.TenantId && s.Name.ToLower() == "admin");
            return isAdmin;
        }
    }
}


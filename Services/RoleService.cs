using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace FutureReady.Services
{
    public interface IRoleService
    {
        Task<bool> IsCurrentUserInRoleAsync(string role);
        Task<bool> IsUserInRoleAsync(ClaimsPrincipal principal, string role);
        Task<string[]> GetCurrentUserRolesAsync();
        Task<string[]> GetUserRolesAsync(ClaimsPrincipal principal);
    }

    public class RoleService : IRoleService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserProvider? _userProvider;
        private readonly IConfiguration _configuration;

        public RoleService(IHttpContextAccessor httpContextAccessor, IUserProvider? userProvider, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userProvider = userProvider;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public Task<bool> IsCurrentUserInRoleAsync(string role)
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            return IsUserInRoleAsync(principal, role);
        }

        public Task<bool> IsUserInRoleAsync(ClaimsPrincipal principal, string role)
        {
            if (principal == null) return Task.FromResult(false);
            if (string.IsNullOrWhiteSpace(role)) return Task.FromResult(false);

            // 1) Check role claims
            var roleClaims = principal.Claims.Where(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)
                                                       || string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)
                                                       || string.Equals(c.Type, "roles", StringComparison.OrdinalIgnoreCase))
                                            .Select(c => c.Value?.Trim())
                                            .Where(v => !string.IsNullOrWhiteSpace(v))
                                            .ToArray();
            if (roleClaims.Any(rc => string.Equals(rc, role, StringComparison.OrdinalIgnoreCase)))
                return Task.FromResult(true);

            // 2) Check configuration mapping Roles:{role}:Users -> [usernames/emails]
            var configuredUsers = _configuration.GetSection($"Roles:{role}:Users").Get<string[]>();
            if (configuredUsers != null && configuredUsers.Length > 0)
            {
                // try IUserProvider username first
                var username = _userProvider?.GetCurrentUsername();
                if (!string.IsNullOrWhiteSpace(username))
                {
                    if (configuredUsers.Any(u => string.Equals(u?.Trim(), username, StringComparison.OrdinalIgnoreCase)))
                        return Task.FromResult(true);
                }

                // fallback to principal name
                var name = principal.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(name) && configuredUsers.Any(u => string.Equals(u?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                    return Task.FromResult(true);

                // fallback to email claim
                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrWhiteSpace(email) && configuredUsers.Any(u => string.Equals(u?.Trim(), email, StringComparison.OrdinalIgnoreCase)))
                    return Task.FromResult(true);

                return Task.FromResult(false);
            }

            // 3) No matching evidence of role -> false
            return Task.FromResult(false);
        }

        public Task<string[]> GetCurrentUserRolesAsync()
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            return GetUserRolesAsync(principal);
        }

        public Task<string[]> GetUserRolesAsync(ClaimsPrincipal principal)
        {
            if (principal == null) return Task.FromResult(Array.Empty<string>());

            var roleClaims = principal.Claims.Where(c => string.Equals(c.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)
                                                       || string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)
                                                       || string.Equals(c.Type, "roles", StringComparison.OrdinalIgnoreCase))
                                            .Select(c => c.Value?.Trim())
                                            .Where(v => !string.IsNullOrWhiteSpace(v))
                                            .ToList();

            // Include configured roles where user is listed
            var configRoles = _configuration.GetSection("Roles").GetChildren();
            foreach (var roleSection in configRoles)
            {
                var roleName = roleSection.Key;
                var users = roleSection.GetSection("Users").Get<string[]>();
                if (users == null || users.Length == 0) continue;

                var username = _userProvider?.GetCurrentUsername();
                if (!string.IsNullOrWhiteSpace(username) && users.Any(u => string.Equals(u?.Trim(), username, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!roleClaims.Contains(roleName, StringComparer.OrdinalIgnoreCase)) roleClaims.Add(roleName);
                    continue;
                }

                var name = principal.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(name) && users.Any(u => string.Equals(u?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!roleClaims.Contains(roleName, StringComparer.OrdinalIgnoreCase)) roleClaims.Add(roleName);
                    continue;
                }

                var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                if (!string.IsNullOrWhiteSpace(email) && users.Any(u => string.Equals(u?.Trim(), email, StringComparison.OrdinalIgnoreCase)))
                {
                    if (!roleClaims.Contains(roleName, StringComparer.OrdinalIgnoreCase)) roleClaims.Add(roleName);
                }
            }

            return Task.FromResult(roleClaims.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        }
    }
}


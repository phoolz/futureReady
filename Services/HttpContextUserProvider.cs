using Microsoft.AspNetCore.Http;

namespace FutureReady.Services
{
    public class HttpContextUserProvider : IUserProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextUserProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetCurrentUsername()
        {
            var name = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(name)) return name;
            // Try other claims, e.g. email
            var email = _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
            return string.IsNullOrWhiteSpace(email) ? null : email;
        }
    }
}


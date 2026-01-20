using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Security.Claims;
using FutureReady.Data;

namespace FutureReady.Services
{
    public class HttpContextTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceProvider _serviceProvider;
        private const string CacheKey = "__CurrentTenantId";

        public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceProvider = serviceProvider;
        }

        public Guid? GetCurrentTenantId()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null) return null;

            // Check per-request cache first
            if (ctx.Items.TryGetValue(CacheKey, out var cached))
                return cached as Guid?;

            // Get user ID from claims
            var userIdClaim = ctx.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                ctx.Items[CacheKey] = null;
                return null;
            }

            // Look up tenant from database
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = dbContext.Users.AsNoTracking().FirstOrDefault(u => u.Id == userId);

            var tenantId = user?.TenantId;
            ctx.Items[CacheKey] = tenantId;
            return tenantId;
        }
    }
}

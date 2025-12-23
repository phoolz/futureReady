using Microsoft.AspNetCore.Http;
using System;

namespace FutureReady.Services
{
    public class HttpContextTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? GetCurrentTenantId()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null) return null;

            // Look for a claim named tenant or tenant_id, or a header X-Tenant-Id
            var claim = ctx.User?.FindFirst("tenant")?.Value ?? ctx.User?.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(claim, out var parsed)) return parsed;

            if (ctx.Request.Headers.TryGetValue("X-Tenant-Id", out var headerVals))
            {
                if (Guid.TryParse(headerVals.FirstOrDefault(), out parsed)) return parsed;
            }

            return null;
        }
    }
}


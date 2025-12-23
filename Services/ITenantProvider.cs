using System;

namespace FutureReady.Services
{
    public interface ITenantProvider
    {
        /// <summary>
        /// Returns the current tenant id or null if not available.
        /// </summary>
        Guid? GetCurrentTenantId();
    }
}


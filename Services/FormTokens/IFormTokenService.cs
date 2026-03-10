using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apiary.Models.School;

namespace Apiary.Services.FormTokens
{
    public interface IFormTokenService
    {
        /// <summary>
        /// Creates a new form token for a placement (14-day expiry)
        /// </summary>
        /// <param name="placementId">The placement ID</param>
        /// <param name="formType">The form type (e.g., "employer_acceptance", "parent_permission")</param>
        /// <param name="email">Optional email for the recipient</param>
        /// <param name="studentId">Student ID for parent forms (null for employer forms)</param>
        /// <param name="tenantId">Optional tenant ID override</param>
        Task<FormToken> GenerateTokenAsync(Guid placementId, string formType, string? email = null, Guid? studentId = null, Guid? tenantId = null);

        /// <summary>
        /// Validates a token and returns it if valid (exists, not expired, not used)
        /// Does not require tenant context - token is the auth
        /// </summary>
        Task<FormToken?> ValidateTokenAsync(string token);

        /// <summary>
        /// Marks a token as used by setting UsedAt timestamp
        /// </summary>
        Task MarkAsUsedAsync(string token);

        /// <summary>
        /// Revokes a token by marking it as deleted
        /// </summary>
        Task RevokeTokenAsync(string token);

        /// <summary>
        /// Revokes a token by ID
        /// </summary>
        Task RevokeTokenByIdAsync(Guid id, Guid? tenantId = null);

        /// <summary>
        /// Gets all tokens for a placement
        /// </summary>
        Task<List<FormToken>> GetByPlacementAsync(Guid placementId, Guid? tenantId = null);
    }
}

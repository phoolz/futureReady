using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apiary.Models.School;

namespace Apiary.Services.StudentAccountTokens
{
    public interface IStudentAccountTokenService
    {
        /// <summary>
        /// Creates a new account token for a student (7-day expiry).
        /// Email is taken from the Student record.
        /// </summary>
        Task<StudentAccountToken> GenerateTokenAsync(Guid studentId, Guid? tenantId = null);

        /// <summary>
        /// Validates a token and returns it with Student included if found.
        /// Does not require tenant context - token is the auth.
        /// </summary>
        Task<StudentAccountToken?> ValidateTokenAsync(string token);

        /// <summary>
        /// Marks a token as used by setting UsedAt timestamp.
        /// </summary>
        Task MarkAsUsedAsync(string token);

        /// <summary>
        /// Revokes a token by marking it as deleted.
        /// </summary>
        Task RevokeTokenByIdAsync(Guid id, Guid? tenantId = null);

        /// <summary>
        /// Gets all tokens for a student.
        /// </summary>
        Task<List<StudentAccountToken>> GetByStudentIdAsync(Guid studentId, Guid? tenantId = null);
    }
}

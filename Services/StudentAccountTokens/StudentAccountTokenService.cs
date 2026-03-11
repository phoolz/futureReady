using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Apiary.Data;
using Apiary.Models.School;

namespace Apiary.Services.StudentAccountTokens
{
    public class StudentAccountTokenService : IStudentAccountTokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;
        private const int TokenExpirationDays = 7;
        private const int TokenByteLength = 32;

        public StudentAccountTokenService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<StudentAccountToken> GenerateTokenAsync(Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating an account token.");

            // Verify student exists and has an email
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == studentId && s.TenantId == tenantId.Value);

            if (student == null)
                throw new InvalidOperationException("Student not found.");

            if (string.IsNullOrWhiteSpace(student.Email))
                throw new InvalidOperationException("Student must have an email address to generate an activation link.");

            var token = GenerateUrlSafeToken();
            var accountToken = new StudentAccountToken
            {
                StudentId = studentId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(TokenExpirationDays),
                TenantId = tenantId.Value
            };

            _context.StudentAccountTokens.Add(accountToken);
            await _context.SaveChangesAsync();

            return accountToken;
        }

        public async Task<StudentAccountToken?> ValidateTokenAsync(string token)
        {
            // Bypass soft delete filter to get the raw token
            var accountToken = await _context.StudentAccountTokens
                .IgnoreQueryFilters()
                .Include(t => t.Student)
                .FirstOrDefaultAsync(t => t.Token == token);

            if (accountToken == null)
                return null;

            // Return the token even if invalid - caller can check IsValid property
            // and handle expired/used cases differently
            return accountToken;
        }

        public async Task MarkAsUsedAsync(string token)
        {
            var accountToken = await _context.StudentAccountTokens
                .FirstOrDefaultAsync(t => t.Token == token);

            if (accountToken != null)
            {
                accountToken.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeTokenByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var accountToken = await _context.StudentAccountTokens
                .FirstOrDefaultAsync(t => t.Id == id && (!tenantId.HasValue || t.TenantId == tenantId.Value));

            if (accountToken != null)
            {
                _context.StudentAccountTokens.Remove(accountToken); // Soft delete via DbContext
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<StudentAccountToken>> GetByStudentIdAsync(Guid studentId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.StudentAccountTokens
                .AsNoTracking()
                .Where(t => t.StudentId == studentId && (!tenantId.HasValue || t.TenantId == tenantId.Value))
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        private static string GenerateUrlSafeToken()
        {
            var bytes = new byte[TokenByteLength];
            RandomNumberGenerator.Fill(bytes);
            // URL-safe Base64: replace + with -, / with _, and remove trailing =
            return Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}

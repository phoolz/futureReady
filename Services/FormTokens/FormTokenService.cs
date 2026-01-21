using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;

namespace FutureReady.Services.FormTokens
{
    public class FormTokenService : IFormTokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;
        private const int TokenExpirationDays = 14;
        private const int TokenByteLength = 32;

        public FormTokenService(ApplicationDbContext context, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public async Task<FormToken> GenerateTokenAsync(Guid placementId, string formType, string? email = null, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            if (!tenantId.HasValue)
                throw new InvalidOperationException("Tenant must be known when creating a form token.");

            var token = GenerateUrlSafeToken();
            var formToken = new FormToken
            {
                PlacementId = placementId,
                Token = token,
                FormType = formType,
                Email = email,
                ExpiresAt = DateTime.UtcNow.AddDays(TokenExpirationDays),
                TenantId = tenantId.Value
            };

            _context.FormTokens.Add(formToken);
            await _context.SaveChangesAsync();

            return formToken;
        }

        public async Task<FormToken?> ValidateTokenAsync(string token)
        {
            // Bypass soft delete filter to get the raw token
            var formToken = await _context.FormTokens
                .IgnoreQueryFilters()
                .Include(ft => ft.Placement)
                .FirstOrDefaultAsync(ft => ft.Token == token);

            if (formToken == null)
                return null;

            // Return the token even if invalid - caller can check IsValid property
            // and handle expired/used cases differently
            return formToken;
        }

        public async Task MarkAsUsedAsync(string token)
        {
            var formToken = await _context.FormTokens
                .FirstOrDefaultAsync(ft => ft.Token == token);

            if (formToken != null)
            {
                formToken.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeTokenAsync(string token)
        {
            var formToken = await _context.FormTokens
                .FirstOrDefaultAsync(ft => ft.Token == token);

            if (formToken != null)
            {
                _context.FormTokens.Remove(formToken); // Soft delete via DbContext
                await _context.SaveChangesAsync();
            }
        }

        public async Task RevokeTokenByIdAsync(Guid id, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            var formToken = await _context.FormTokens
                .FirstOrDefaultAsync(ft => ft.Id == id && (!tenantId.HasValue || ft.TenantId == tenantId.Value));

            if (formToken != null)
            {
                _context.FormTokens.Remove(formToken); // Soft delete via DbContext
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<FormToken>> GetByPlacementAsync(Guid placementId, Guid? tenantId = null)
        {
            tenantId ??= _tenantProvider?.GetCurrentTenantId();
            return await _context.FormTokens
                .AsNoTracking()
                .Where(ft => ft.PlacementId == placementId && (!tenantId.HasValue || ft.TenantId == tenantId.Value))
                .OrderByDescending(ft => ft.CreatedAt)
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

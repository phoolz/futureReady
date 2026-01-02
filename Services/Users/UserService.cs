using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models;

namespace FutureReady.Services.Users
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users.AsNoTracking().ToListAsync();
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task CreateAsync(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = PasswordHasher.HashPassword(user.PasswordHash);
            }
            else
            {
                user.PasswordHash = null;
            }

            _context.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user, byte[]? rowVersion = null)
        {
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existing == null) throw new InvalidOperationException("User not found");

            existing.UserName = user.UserName;
            existing.DisplayName = user.DisplayName;
            existing.Email = user.Email;

            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                existing.PasswordHash = PasswordHasher.HashPassword(user.PasswordHash);
            }

            existing.IsActive = user.IsActive;
            existing.ExternalId = user.ExternalId;

            if (rowVersion != null)
                _context.Entry(existing).Property("RowVersion").OriginalValue = rowVersion;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}


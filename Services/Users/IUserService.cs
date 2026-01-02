namespace FutureReady.Services.Users;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FutureReady.Models;

public interface IUserService
{
    Task<List<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task CreateAsync(User user);
    Task UpdateAsync(User user, byte[]? rowVersion = null);
    Task DeleteAsync(Guid id);
}
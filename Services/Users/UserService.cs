using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Apiary.Data;
using Apiary.Models;
using Apiary.Models.Tables;

namespace Apiary.Services.Users;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<UserViewModel>> GetAllAsync()
    {
        var users = await _userManager.Users
            .Where(u => !u.IsDeleted)
            .ToListAsync();

        var schools = await _context.Schools.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Name);

        var viewModels = new List<UserViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            viewModels.Add(new UserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email,
                DisplayName = user.DisplayName,
                IsActive = user.IsActive,
                TenantId = user.TenantId,
                TenantName = schools.GetValueOrDefault(user.TenantId),
                RoleName = string.Join(", ", roles)
            });
        }

        return viewModels;
    }

    public async Task<PagedResult<UserViewModel>> GetPagedAsync(TableQuery tableQuery)
    {
        // Get all users (we need to get roles for each, so fetching all first)
        var allViewModels = await GetAllAsync();

        // Search
        if (!string.IsNullOrWhiteSpace(tableQuery.Search))
        {
            var search = tableQuery.Search.ToLower();
            allViewModels = allViewModels.Where(u =>
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(search)) ||
                u.UserName.ToLower().Contains(search) ||
                (u.Email != null && u.Email.ToLower().Contains(search))).ToList();
        }

        var totalCount = allViewModels.Count;

        // Sort
        allViewModels = tableQuery.Sort?.ToLower() switch
        {
            "email" => tableQuery.IsDescending
                ? allViewModels.OrderByDescending(u => u.Email).ToList()
                : allViewModels.OrderBy(u => u.Email).ToList(),
            "role" => tableQuery.IsDescending
                ? allViewModels.OrderByDescending(u => u.RoleName).ToList()
                : allViewModels.OrderBy(u => u.RoleName).ToList(),
            _ => tableQuery.IsDescending
                ? allViewModels.OrderByDescending(u => u.DisplayName ?? u.UserName).ToList()
                : allViewModels.OrderBy(u => u.DisplayName ?? u.UserName).ToList()
        };

        // Page
        var items = allViewModels
            .Skip((tableQuery.Page - 1) * tableQuery.Size)
            .Take(tableQuery.Size)
            .ToList();

        return new PagedResult<UserViewModel>
        {
            Items = items,
            TotalCount = totalCount,
            Page = tableQuery.Page,
            Size = tableQuery.Size
        };
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null || user.IsDeleted) return null;
        return user;
    }

    public async Task<string?> GetRoleAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return roles.FirstOrDefault();
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateAsync(CreateUserModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            DisplayName = model.DisplayName,
            IsActive = model.IsActive,
            TenantId = model.TenantId
        };

        var result = await _userManager.CreateAsync(user, model.Password ?? string.Empty);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.RoleName);
            return (true, Enumerable.Empty<string>());
        }

        return (false, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateAsync(EditUserModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id.ToString());
        if (user == null || user.IsDeleted)
            return (false, new[] { "User not found" });

        user.UserName = model.UserName;
        user.Email = model.Email;
        user.DisplayName = model.DisplayName;
        user.IsActive = model.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description));

        // Update role if changed
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(model.RoleName))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.RoleName);
        }

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!passwordResult.Succeeded)
                return (false, passwordResult.Errors.Select(e => e.Description));
        }

        return (true, Enumerable.Empty<string>());
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return false;

        user.IsDeleted = true;
        await _userManager.UpdateAsync(user);
        return true;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateProfileAsync(ProfileModel model)
    {
        var user = await _userManager.FindByIdAsync(model.Id.ToString());
        if (user == null || user.IsDeleted)
            return (false, new[] { "User not found" });

        user.DisplayName = model.DisplayName;
        user.Email = model.Email;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return (false, result.Errors.Select(e => e.Description));

        return (true, Enumerable.Empty<string>());
    }
}

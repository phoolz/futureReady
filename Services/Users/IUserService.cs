using Apiary.Models;
using Apiary.Models.Tables;

namespace Apiary.Services.Users;

public interface IUserService
{
    Task<List<UserViewModel>> GetAllAsync();
    Task<PagedResult<UserViewModel>> GetPagedAsync(TableQuery query);
    Task<ApplicationUser?> GetByIdAsync(Guid id);
    Task<string?> GetRoleAsync(Guid userId);
    Task<(bool Success, IEnumerable<string> Errors)> CreateAsync(CreateUserModel model);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateAsync(EditUserModel model);
    Task<bool> DeleteAsync(Guid id);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateProfileAsync(ProfileModel model);
}

public class UserViewModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; }
    public Guid TenantId { get; set; }
    public string? TenantName { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class CreateUserModel
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Password { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid TenantId { get; set; }
    public string RoleName { get; set; } = Roles.Teacher;
}

public class EditUserModel
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? NewPassword { get; set; }
    public bool IsActive { get; set; }
    public string RoleName { get; set; } = Roles.Teacher;
}

public class ProfileModel
{
    public Guid Id { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
}

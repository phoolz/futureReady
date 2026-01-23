using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models;
using FutureReady.Services;

namespace FutureReady.Controllers
{
    [Authorize(Roles = Roles.SiteAdmin)]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserProvider _userProvider;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IUserProvider userProvider)
        {
            _context = context;
            _userManager = userManager;
            _userProvider = userProvider;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .Where(u => !u.IsDeleted)
                .ToListAsync();

            var userViewModels = new List<UserIndexViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserIndexViewModel
                {
                    User = user,
                    RoleName = string.Join(", ", roles)
                });
            }

            return View(userViewModels);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id.Value.ToString());

            if (user == null || user.IsDeleted) return NotFound();

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            ViewData["SchoolId"] = new SelectList(_context.Schools.AsNoTracking(), "Id", "Name");
            ViewData["Roles"] = new SelectList(Roles.AllRoles);
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
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
                    // Assign role to the user
                    await _userManager.AddToRoleAsync(user, model.RoleName);
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewData["SchoolId"] = new SelectList(_context.Schools.AsNoTracking(), "Id", "Name", model.TenantId);
            ViewData["Roles"] = new SelectList(Roles.AllRoles);
            return View(model);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id.Value.ToString());
            if (user == null || user.IsDeleted) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? Roles.Teacher;

            var model = new EditUserViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                IsActive = user.IsActive,
                RoleName = currentRole
            };

            ViewData["Roles"] = new SelectList(Roles.AllRoles);
            return View(model);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditUserViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["Roles"] = new SelectList(Roles.AllRoles);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null || user.IsDeleted) return NotFound();

            user.UserName = model.UserName;
            user.Email = model.Email;
            user.DisplayName = model.DisplayName;
            user.IsActive = model.IsActive;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // Update role if changed
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(model.RoleName))
                {
                    // Remove all existing roles
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    // Add new role
                    await _userManager.AddToRoleAsync(user, model.RoleName);
                }

                // Update password if provided
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        ViewData["Roles"] = new SelectList(Roles.AllRoles);
                        return View(model);
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewData["Roles"] = new SelectList(Roles.AllRoles);
            return View(model);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id.Value.ToString());
            if (user == null || user.IsDeleted) return NotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
            {
                // Soft delete
                user.IsDeleted = true;
                await _userManager.UpdateAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Profile
        [Authorize] // Override class-level authorization - allow all authenticated users
        public async Task<IActionResult> Profile()
        {
            var username = _userProvider.GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var user = await _userManager.FindByNameAsync(username)
                       ?? await _userManager.FindByEmailAsync(username);

            if (user == null || user.IsDeleted)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Profile
        [Authorize] // Override class-level authorization - allow all authenticated users
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var username = _userProvider.GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var user = await _userManager.FindByNameAsync(username)
                       ?? await _userManager.FindByEmailAsync(username);

            if (user == null || user.IsDeleted || user.Id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            user.DisplayName = model.DisplayName;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(user);
        }
    }

    public class CreateUserViewModel
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? Password { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid TenantId { get; set; }
        public string RoleName { get; set; } = Roles.Teacher;
    }

    public class EditUserViewModel
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string? NewPassword { get; set; }
        public bool IsActive { get; set; }
        public string RoleName { get; set; } = Roles.Teacher;
    }

    public class UserIndexViewModel
    {
        public ApplicationUser User { get; set; } = null!;
        public string RoleName { get; set; } = string.Empty;
    }

    public class ProfileViewModel
    {
        public Guid Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
    }
}

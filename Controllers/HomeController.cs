using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FutureReady.Models;
using FutureReady.Data;
using FutureReady.Services;
using Microsoft.EntityFrameworkCore;

namespace FutureReady.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider? _tenantProvider;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, ITenantProvider? tenantProvider = null)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var tenantId = _tenantProvider?.GetCurrentTenantId();

        var studentsQuery = _context.Students.AsNoTracking();

        if (tenantId.HasValue)
        {
            studentsQuery = studentsQuery.Where(s => s.TenantId == tenantId.Value);
        }

        var totalStudents = await studentsQuery.CountAsync();

        ViewData["TotalStudents"] = totalStudents;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    // Diagnostic action to check user roles
    public async Task<IActionResult> CheckRoles()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Content("Not authenticated");
        }

        var user = await _userManager.FindByNameAsync(username);
        if (user == null)
        {
            return Content($"User not found: {username}");
        }

        var roles = await _userManager.GetRolesAsync(user);

        var output = $"Username: {username}\n";
        output += $"User ID: {user.Id}\n";
        output += $"Roles assigned: {roles.Count}\n\n";

        foreach (var role in roles)
        {
            output += $"- '{role}'\n";
        }

        output += $"\nRole checks:\n";
        output += $"User.IsInRole(Roles.SiteAdmin): {User.IsInRole(Roles.SiteAdmin)}\n";
        output += $"User.IsInRole(Roles.Teacher): {User.IsInRole(Roles.Teacher)}\n";
        output += $"User.IsInRole(Roles.Student): {User.IsInRole(Roles.Student)}\n";
        output += $"\nExpected role names:\n";
        output += $"Roles.SiteAdmin = '{Roles.SiteAdmin}'\n";
        output += $"Roles.Teacher = '{Roles.Teacher}'\n";
        output += $"Roles.Student = '{Roles.Student}'\n";

        return Content(output, "text/plain");
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

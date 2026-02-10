using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FutureReady.Models;
using FutureReady.Models.StudentPortal;
using FutureReady.Data;
using FutureReady.Services;
using FutureReady.Services.Students;
using FutureReady.Services.Placements;
using FutureReady.Services.LogbookEntries;
using Microsoft.EntityFrameworkCore;

namespace FutureReady.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly ITenantProvider? _tenantProvider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStudentService _studentService;
    private readonly IPlacementService _placementService;
    private readonly ILogbookEntryService _logbookEntryService;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IStudentService studentService,
        IPlacementService placementService,
        ILogbookEntryService logbookEntryService,
        ITenantProvider? tenantProvider = null)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _studentService = studentService;
        _placementService = placementService;
        _logbookEntryService = logbookEntryService;
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

        // Load student placement data if user is a student
        if (User.IsInRole(Roles.Student))
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                var student = await _studentService.GetByUserIdAsync(userId, tenantId);

                if (student == null)
                {
                    ViewData["StudentNotLinked"] = true;
                }
                else
                {
                    var placements = await _placementService.GetByStudentIdAsync(student.Id, tenantId);

                    var placementViewModels = new List<StudentPlacementViewModel>();

                    foreach (var placement in placements)
                    {
                        var totalHours = await _logbookEntryService.GetTotalHoursAsync(placement.Id, tenantId);

                        placementViewModels.Add(new StudentPlacementViewModel
                        {
                            PlacementId = placement.Id,
                            Year = placement.Year,
                            Status = placement.Status,
                            CompanyName = placement.Company?.Name,
                            CompanyIndustry = placement.Company?.Industry,
                            SupervisorFullName = placement.Supervisor?.FullName,
                            SupervisorJobTitle = placement.Supervisor?.JobTitle,
                            SupervisorEmail = placement.Supervisor?.Email,
                            SupervisorPhone = placement.Supervisor?.Phone,
                            WorkStartTime = placement.WorkStartTime,
                            WorkEndTime = placement.WorkEndTime,
                            DressRequirement = placement.DressRequirement,
                            TotalHoursWorked = totalHours
                        });
                    }

                    var studentPortalModel = new StudentPortalViewModel
                    {
                        StudentId = student.Id,
                        StudentDisplayName = student.DisplayName,
                        StudentFullName = student.FullName,
                        ConfirmedPlacements = placementViewModels.Where(p => p.IsConfirmed).ToList(),
                        PendingPlacements = placementViewModels.Where(p => !p.IsConfirmed).ToList()
                    };

                    ViewData["StudentPortalModel"] = studentPortalModel;
                }
            }
        }

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

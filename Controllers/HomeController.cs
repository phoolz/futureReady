using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
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

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, ITenantProvider? tenantProvider = null)
    {
        _logger = logger;
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var tenantId = _tenantProvider?.GetCurrentTenantId();

        var studentsQuery = _context.Students.AsNoTracking();
        var cohortsQuery = _context.Cohorts.AsNoTracking();

        if (tenantId.HasValue)
        {
            studentsQuery = studentsQuery.Where(s => s.TenantId == tenantId.Value);
            cohortsQuery = cohortsQuery.Where(c => c.TenantId == tenantId.Value);
        }

        var totalStudents = await studentsQuery.CountAsync();
        var activeCohorts = await cohortsQuery.CountAsync();

        var currentYear = DateTime.UtcNow.Year;
        var upcomingGraduations = await cohortsQuery
            .Where(c => c.GraduationYear >= currentYear)
            .CountAsync();

        ViewData["TotalStudents"] = totalStudents;
        ViewData["ActiveCohorts"] = activeCohorts;
        ViewData["UpcomingGraduations"] = upcomingGraduations;

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

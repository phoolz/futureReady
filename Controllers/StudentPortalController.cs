using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FutureReady.Models;
using FutureReady.Models.StudentPortal;
using FutureReady.Services;
using FutureReady.Services.Students;
using FutureReady.Services.Placements;
using FutureReady.Services.LogbookEntries;

namespace FutureReady.Controllers
{
    [Authorize(Roles = Roles.Student)]
    public class StudentPortalController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly IPlacementService _placementService;
        private readonly ILogbookEntryService _logbookEntryService;
        private readonly ITenantProvider? _tenantProvider;

        public StudentPortalController(
            IStudentService studentService,
            IPlacementService placementService,
            ILogbookEntryService logbookEntryService,
            ITenantProvider? tenantProvider = null)
        {
            _studentService = studentService;
            _placementService = placementService;
            _logbookEntryService = logbookEntryService;
            _tenantProvider = tenantProvider;
        }

        public async Task<IActionResult> Index()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var student = await _studentService.GetByUserIdAsync(userId, tenantId);

            if (student == null)
            {
                return View("NotLinked");
            }

            var placements = await _placementService.GetByStudentIdAsync(student.Id, tenantId);

            var placementViewModels = new System.Collections.Generic.List<StudentPlacementViewModel>();

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

            var viewModel = new StudentPortalViewModel
            {
                StudentId = student.Id,
                StudentDisplayName = student.DisplayName,
                StudentFullName = student.FullName,
                ConfirmedPlacements = placementViewModels.Where(p => p.IsConfirmed).ToList(),
                PendingPlacements = placementViewModels.Where(p => !p.IsConfirmed).ToList()
            };

            return View(viewModel);
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FutureReady.Models;
using FutureReady.Models.LogbookEntries;
using FutureReady.Services;
using FutureReady.Services.LogbookEntries;
using FutureReady.Services.Placements;
using FutureReady.Services.Students;

namespace FutureReady.Controllers
{
    [Authorize(Roles = Roles.TeacherOrStudent)]
    public class LogbookEntriesController : Controller
    {
        private readonly ILogbookEntryService _logbookService;
        private readonly IPlacementService _placementService;
        private readonly IStudentAuthorizationService _studentAuthService;
        private readonly ITenantProvider? _tenantProvider;

        public LogbookEntriesController(
            ILogbookEntryService logbookService,
            IPlacementService placementService,
            IStudentAuthorizationService studentAuthService,
            ITenantProvider? tenantProvider = null)
        {
            _logbookService = logbookService;
            _placementService = placementService;
            _studentAuthService = studentAuthService;
            _tenantProvider = tenantProvider;
        }

        // GET: LogbookEntries?placementId={guid}
        public async Task<IActionResult> Index(Guid placementId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();

            // Load placement for authorization and display info
            var placement = await _placementService.GetByIdWithDetailsAsync(placementId, tenantId);
            if (placement == null)
                return NotFound();

            // Authorization check: students can only see their own placements
            if (User.IsInRole(Roles.Student))
            {
                var studentId = await _studentAuthService.GetCurrentUserStudentIdAsync();
                if (!studentId.HasValue || placement.StudentId != studentId.Value)
                    return Forbid();
            }

            // Load entries for this placement
            var entries = await _logbookService.GetByPlacementIdAsync(placementId, tenantId);

            // Calculate cumulative hours
            // Entries come ordered by Date descending, so sort ascending for calculation
            var sortedAscending = entries.OrderBy(e => e.Date).ToList();
            decimal runningTotal = 0;
            foreach (var entry in sortedAscending)
            {
                runningTotal += entry.TotalHoursWorked;
                entry.CumulativeHours = runningTotal;
            }

            // Map to view model (display in descending order - most recent first)
            var viewModel = new LogbookEntriesListViewModel
            {
                PlacementId = placementId,
                PlacementInfo = $"{placement.Company?.Name ?? "Unknown Company"} - {placement.Year}",
                TotalHours = runningTotal,
                VerifiedCount = entries.Count(e => e.SupervisorVerified),
                TotalEntries = entries.Count,
                Entries = entries.OrderByDescending(e => e.Date).Select(e => new LogbookEntryViewModel
                {
                    Id = e.Id,
                    Date = e.Date,
                    StartTime = e.StartTime,
                    FinishTime = e.FinishTime,
                    TotalHoursWorked = e.TotalHoursWorked,
                    CumulativeHours = e.CumulativeHours,
                    SupervisorVerified = e.SupervisorVerified
                }).ToList()
            };

            return View(viewModel);
        }
    }
}

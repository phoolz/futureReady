using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Apiary.Models;
using Apiary.Models.LogbookEntries;
using Apiary.Services;
using Apiary.Services.LogbookEntries;
using Apiary.Services.Placements;
using Apiary.Services.PlacementStudents;
using Apiary.Services.Students;

namespace Apiary.Controllers
{
    [Authorize(Roles = Roles.TeacherOrStudent)]
    public class LogbookEntriesController : Controller
    {
        private readonly ILogbookEntryService _logbookService;
        private readonly IPlacementService _placementService;
        private readonly IPlacementStudentService _placementStudentService;
        private readonly IStudentAuthorizationService _studentAuthService;
        private readonly ITenantProvider? _tenantProvider;

        public LogbookEntriesController(
            ILogbookEntryService logbookService,
            IPlacementService placementService,
            IPlacementStudentService placementStudentService,
            IStudentAuthorizationService studentAuthService,
            ITenantProvider? tenantProvider = null)
        {
            _logbookService = logbookService;
            _placementService = placementService;
            _placementStudentService = placementStudentService;
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

            // Get the PlacementStudent for the current user (if student)
            Guid? placementStudentId = null;

            // Authorization check: students can only see their own placements
            if (User.IsInRole(Roles.Student))
            {
                var studentId = await _studentAuthService.GetCurrentUserStudentIdAsync();
                if (!studentId.HasValue)
                    return Forbid();

                var placementStudent = placement.PlacementStudents.FirstOrDefault(ps => ps.StudentId == studentId.Value && !ps.IsDeleted);
                if (placementStudent == null)
                    return Forbid();

                placementStudentId = placementStudent.Id;
            }
            else
            {
                // For teachers, show entries for the first student (or could aggregate)
                var firstPlacementStudent = placement.PlacementStudents.FirstOrDefault(ps => !ps.IsDeleted);
                placementStudentId = firstPlacementStudent?.Id;
            }

            if (!placementStudentId.HasValue)
            {
                // No students in placement
                var emptyViewModel = new LogbookEntriesListViewModel
                {
                    PlacementId = placementId,
                    PlacementInfo = $"{placement.Company?.Name ?? "Unknown Company"} - {placement.Year}",
                    TotalHours = 0,
                    VerifiedCount = 0,
                    TotalEntries = 0,
                    Entries = new()
                };
                return View(emptyViewModel);
            }

            // Load entries for this placement student
            var entries = await _logbookService.GetByPlacementStudentIdAsync(placementStudentId.Value, tenantId);

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

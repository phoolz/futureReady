using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Apiary.Models;
using Apiary.Models.School;
using Apiary.Services;
using Apiary.Services.Placements;
using Apiary.Services.PlacementStudents;
using Apiary.Services.Students;
using Apiary.Services.Companies;
using Apiary.Services.Supervisors;
using Apiary.Services.FormTokens;
using Apiary.Services.LogbookEntries;
using Apiary.Models.LogbookEntries;

namespace Apiary.Controllers
{
    [Authorize(Roles = Roles.TeacherOrStudent)]
    public class PlacementsController : Controller
    {
        private readonly IPlacementService _placementService;
        private readonly IPlacementStudentService _placementStudentService;
        private readonly IStudentService _studentService;
        private readonly ICompanyService _companyService;
        private readonly ISupervisorService _supervisorService;
        private readonly IFormTokenService _formTokenService;
        private readonly IStudentAuthorizationService _studentAuthService;
        private readonly ILogbookEntryService _logbookService;
        private readonly ITenantProvider? _tenantProvider;

        public PlacementsController(
            IPlacementService placementService,
            IPlacementStudentService placementStudentService,
            IStudentService studentService,
            ICompanyService companyService,
            ISupervisorService supervisorService,
            IFormTokenService formTokenService,
            IStudentAuthorizationService studentAuthService,
            ILogbookEntryService logbookService,
            ITenantProvider? tenantProvider = null)
        {
            _placementService = placementService;
            _placementStudentService = placementStudentService;
            _studentService = studentService;
            _companyService = companyService;
            _supervisorService = supervisorService;
            _formTokenService = formTokenService;
            _studentAuthService = studentAuthService;
            _logbookService = logbookService;
            _tenantProvider = tenantProvider;
        }

        // GET: Placements
        public async Task<IActionResult> Index()
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();

            if (User.IsInRole(Roles.Student))
            {
                var studentId = await _studentAuthService.GetCurrentUserStudentIdAsync();
                if (!studentId.HasValue)
                    return Forbid();
                var placements = await _placementService.GetByStudentIdAsync(studentId.Value, tenantId);
                return View(placements);
            }

            var allPlacements = await _placementService.GetAllAsync(tenantId);
            return View(allPlacements);
        }

        // GET: Placements/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id.Value, tenantId);
            if (placement == null) return NotFound();

            // Students can only view placements they're assigned to
            if (User.IsInRole(Roles.Student))
            {
                var studentId = await _studentAuthService.GetCurrentUserStudentIdAsync();
                if (!studentId.HasValue || !placement.PlacementStudents.Any(ps => ps.StudentId == studentId.Value))
                    return Forbid();
            }

            // Get form tokens for this placement
            var formTokens = await _formTokenService.GetByPlacementAsync(id.Value, tenantId);
            ViewData["FormTokens"] = formTokens;

            // Get logbook entries for the first student (for student view) or summary
            var placementStudents = placement.PlacementStudents.Where(ps => !ps.IsDeleted).ToList();
            if (placementStudents.Any())
            {
                var firstPlacementStudent = placementStudents.First();
                var logbookEntries = await _logbookService.GetByPlacementStudentIdAsync(firstPlacementStudent.Id, tenantId);
                var sortedEntries = logbookEntries.OrderBy(e => e.Date).ToList();

                // Calculate cumulative hours
                decimal cumulative = 0;
                foreach (var entry in sortedEntries)
                {
                    cumulative += entry.TotalHoursWorked;
                    entry.CumulativeHours = cumulative;
                }

                var logbookViewModel = new LogbookEntriesListViewModel
                {
                    PlacementId = id.Value,
                    TotalHours = cumulative,
                    VerifiedCount = sortedEntries.Count(e => e.SupervisorVerified),
                    TotalEntries = sortedEntries.Count,
                    Entries = sortedEntries.OrderByDescending(e => e.Date).Take(5).Select(e => new LogbookEntryViewModel
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
                ViewData["LogbookEntries"] = logbookViewModel;
            }

            return View(placement);
        }

        // GET: Placements/Create
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: Placements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> Create([Bind("CompanyId,SupervisorId,Year,Status")] Placement placement)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(placement.CompanyId, placement.SupervisorId);
                return View(placement);
            }

            try
            {
                await _placementService.CreateAsync(placement);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateDropdowns(placement.CompanyId, placement.SupervisorId);
                return View(placement);
            }

            return RedirectToAction(nameof(Edit), new { id = placement.Id });
        }

        // GET: Placements/Edit/5
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id.Value, tenantId);
            if (placement == null) return NotFound();

            await PopulateDropdowns(placement.CompanyId, placement.SupervisorId);

            // Get all students for dropdown
            var students = await _studentService.GetAllAsync(tenantId);
            var assignedStudentIds = placement.PlacementStudents.Where(ps => !ps.IsDeleted).Select(ps => ps.StudentId).ToList();
            var availableStudents = students.Where(s => !assignedStudentIds.Contains(s.Id)).ToList();
            ViewData["AvailableStudents"] = new SelectList(
                availableStudents.Select(s => new { s.Id, FullName = $"{s.FirstName} {s.LastName}" }),
                "Id", "FullName");

            return View(placement);
        }

        // POST: Placements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CompanyId,SupervisorId,Year,Status,PlacementRole,RowVersion")] Placement placement)
        {
            if (id != placement.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(placement.CompanyId, placement.SupervisorId);
                return View(placement);
            }

            try
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                await _placementService.UpdateAsync(placement, placement.RowVersion, tenantId);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _placementService.ExistsAsync(id, _tenantProvider?.GetCurrentTenantId()))
                    return NotFound();
                else
                    throw;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateDropdowns(placement.CompanyId, placement.SupervisorId);
                return View(placement);
            }
        }

        // POST: Placements/AddStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> AddStudent(Guid id, Guid studentId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();

            try
            {
                await _placementStudentService.AddStudentToPlacementAsync(id, studentId, tenantId);
                await _placementService.RecalculateStatusAsync(id, tenantId);
                TempData["SuccessMessage"] = "Student added to placement successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Edit), new { id });
        }

        // POST: Placements/RemoveStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> RemoveStudent(Guid id, Guid studentId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();

            await _placementStudentService.RemoveStudentFromPlacementAsync(id, studentId, tenantId);
            await _placementService.RecalculateStatusAsync(id, tenantId);
            TempData["SuccessMessage"] = "Student removed from placement successfully.";

            return RedirectToAction(nameof(Edit), new { id });
        }

        // GET: Placements/Delete/5
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id.Value, tenantId);
            if (placement == null) return NotFound();

            return View(placement);
        }

        // POST: Placements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            await _placementService.DeleteAsync(id, tenantId);
            return RedirectToAction(nameof(Index));
        }

        // POST: Placements/SendEmployerForm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> SendEmployerForm(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id, tenantId);
            if (placement == null) return NotFound();

            // Revoke all existing active employer form tokens
            var existingTokens = await _formTokenService.GetByPlacementAsync(id, tenantId);
            foreach (var existingToken in existingTokens.Where(t => t.FormType == "employer_acceptance" && t.IsValid))
            {
                await _formTokenService.RevokeTokenByIdAsync(existingToken.Id, tenantId);
            }

            var email = placement.Supervisor?.Email;
            var formToken = await _formTokenService.GenerateTokenAsync(id, "employer_acceptance", email, null, tenantId);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var formUrl = $"{baseUrl}/employer/form/{formToken.Token}";
            TempData["FormLink"] = formUrl;
            TempData["FormLinkMessage"] = "Employer form link generated. Copy and send to the employer:";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Placements/DeleteEmployerFormToken
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> DeleteEmployerFormToken(Guid id, Guid tokenId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            await _formTokenService.RevokeTokenByIdAsync(tokenId, tenantId);
            TempData["SuccessMessage"] = "Form token deleted successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Placements/ResendEmployerForm
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> ResendEmployerForm(Guid id, Guid tokenId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id, tenantId);
            if (placement == null) return NotFound();

            // Revoke the old token
            await _formTokenService.RevokeTokenByIdAsync(tokenId, tenantId);

            // Generate a new token
            var email = placement.Supervisor?.Email;
            var formToken = await _formTokenService.GenerateTokenAsync(id, "employer_acceptance", email, null, tenantId);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var formUrl = $"{baseUrl}/employer/form/{formToken.Token}";
            TempData["FormLink"] = formUrl;
            TempData["FormLinkMessage"] = "New employer form link generated (previous link revoked). Copy and send to the employer:";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Placements/EmployerFormReview/5
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> EmployerFormReview(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id.Value, tenantId);
            if (placement == null) return NotFound();

            return View(placement);
        }

        // POST: Placements/SendParentFormForStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> SendParentFormForStudent(Guid id, Guid studentId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id, tenantId);
            if (placement == null) return NotFound();

            // Verify student is in this placement
            var placementStudent = placement.PlacementStudents.FirstOrDefault(ps => ps.StudentId == studentId && !ps.IsDeleted);
            if (placementStudent == null)
            {
                TempData["ErrorMessage"] = "Student is not assigned to this placement.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Revoke all existing active parent form tokens for this student
            var existingTokens = await _formTokenService.GetByPlacementAsync(id, tenantId);
            foreach (var existingToken in existingTokens.Where(t => t.FormType == "parent_permission" && t.StudentId == studentId && t.IsValid))
            {
                await _formTokenService.RevokeTokenByIdAsync(existingToken.Id, tenantId);
            }

            // Generate new token with StudentId
            var formToken = await _formTokenService.GenerateTokenAsync(id, "parent_permission", null, studentId, tenantId);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var formUrl = $"{baseUrl}/parent/form/{formToken.Token}";
            TempData["ParentFormLink"] = formUrl;
            TempData["ParentFormLinkMessage"] = $"Parent permission form link generated for {placementStudent.Student?.FullName ?? "student"}. Copy and send to the parent/guardian:";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Placements/SendParentForm/5 - Sends parent forms for ALL students
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> SendParentForm(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id, tenantId);
            if (placement == null) return NotFound();

            var placementStudents = placement.PlacementStudents.Where(ps => !ps.IsDeleted && ps.Status == "pending_parent").ToList();
            if (!placementStudents.Any())
            {
                TempData["ErrorMessage"] = "No students pending parent permission in this placement.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var existingTokens = await _formTokenService.GetByPlacementAsync(id, tenantId);

            foreach (var ps in placementStudents)
            {
                // Revoke existing tokens for this student
                foreach (var existingToken in existingTokens.Where(t => t.FormType == "parent_permission" && t.StudentId == ps.StudentId && t.IsValid))
                {
                    await _formTokenService.RevokeTokenByIdAsync(existingToken.Id, tenantId);
                }

                // Generate new token for this student
                await _formTokenService.GenerateTokenAsync(id, "parent_permission", null, ps.StudentId, tenantId);
            }

            TempData["SuccessMessage"] = $"Parent form links generated for {placementStudents.Count} student(s). View student list for individual links.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Placements/DeleteParentFormToken
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> DeleteParentFormToken(Guid id, Guid tokenId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            await _formTokenService.RevokeTokenByIdAsync(tokenId, tenantId);
            TempData["SuccessMessage"] = "Parent form token deleted successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Placements/ResendParentForm
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Teacher)]
        public async Task<IActionResult> ResendParentForm(Guid id, Guid tokenId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id, tenantId);
            if (placement == null) return NotFound();

            // Get the old token to find the StudentId
            var existingTokens = await _formTokenService.GetByPlacementAsync(id, tenantId);
            var oldToken = existingTokens.FirstOrDefault(t => t.Id == tokenId);
            var studentId = oldToken?.StudentId;

            // Revoke the old token
            await _formTokenService.RevokeTokenByIdAsync(tokenId, tenantId);

            // Generate a new token
            var formToken = await _formTokenService.GenerateTokenAsync(id, "parent_permission", null, studentId, tenantId);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var formUrl = $"{baseUrl}/parent/form/{formToken.Token}";
            TempData["ParentFormLink"] = formUrl;
            TempData["ParentFormLinkMessage"] = "New parent form link generated (previous link revoked). Copy and send to the parent/guardian:";

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task PopulateDropdowns(Guid? companyId = null, Guid? supervisorId = null)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();

            var companies = await _companyService.GetAllAsync(tenantId);
            ViewData["CompanyId"] = new SelectList(companies, "Id", "Name", companyId);

            var supervisors = await _supervisorService.GetAllAsync(tenantId);
            ViewData["SupervisorId"] = new SelectList(
                supervisors.Select(s => new { s.Id, FullName = $"{s.FirstName} {s.LastName}" }),
                "Id", "FullName", supervisorId);
        }
    }
}

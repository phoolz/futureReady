using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FutureReady.Models.School;
using FutureReady.Services;
using FutureReady.Services.Placements;
using FutureReady.Services.Students;
using FutureReady.Services.Companies;
using FutureReady.Services.Supervisors;
using FutureReady.Services.FormTokens;

namespace FutureReady.Controllers
{
    [Authorize]
    public class PlacementsController : Controller
    {
        private readonly IPlacementService _placementService;
        private readonly IStudentService _studentService;
        private readonly ICompanyService _companyService;
        private readonly ISupervisorService _supervisorService;
        private readonly IFormTokenService _formTokenService;
        private readonly ITenantProvider? _tenantProvider;

        public PlacementsController(
            IPlacementService placementService,
            IStudentService studentService,
            ICompanyService companyService,
            ISupervisorService supervisorService,
            IFormTokenService formTokenService,
            ITenantProvider? tenantProvider = null)
        {
            _placementService = placementService;
            _studentService = studentService;
            _companyService = companyService;
            _supervisorService = supervisorService;
            _formTokenService = formTokenService;
            _tenantProvider = tenantProvider;
        }

        // GET: Placements
        public async Task<IActionResult> Index()
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placements = await _placementService.GetAllAsync(tenantId);
            return View(placements);
        }

        // GET: Placements/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id.Value, tenantId);
            if (placement == null) return NotFound();

            // Get form tokens for this placement
            var formTokens = await _formTokenService.GetByPlacementAsync(id.Value, tenantId);
            ViewData["FormTokens"] = formTokens;

            return View(placement);
        }

        // GET: Placements/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: Placements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentId,CompanyId,SupervisorId,Year,Status")] Placement placement)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(placement.StudentId, placement.CompanyId, placement.SupervisorId);
                return View(placement);
            }

            try
            {
                await _placementService.CreateAsync(placement);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateDropdowns(placement.StudentId, placement.CompanyId, placement.SupervisorId);
                return View(placement);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Placements/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdAsync(id.Value, tenantId);
            if (placement == null) return NotFound();

            await PopulateDropdowns(placement.StudentId, placement.CompanyId, placement.SupervisorId);
            return View(placement);
        }

        // POST: Placements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,StudentId,CompanyId,SupervisorId,Year,Status,RowVersion")] Placement placement)
        {
            if (id != placement.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(placement.StudentId, placement.CompanyId, placement.SupervisorId);
                return View(placement);
            }

            try
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                await _placementService.UpdateAsync(placement, placement.RowVersion, tenantId);
                return RedirectToAction(nameof(Index));
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
                await PopulateDropdowns(placement.StudentId, placement.CompanyId, placement.SupervisorId);
                return View(placement);
            }
        }

        // GET: Placements/Delete/5
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
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            await _placementService.DeleteAsync(id, tenantId);
            return RedirectToAction(nameof(Index));
        }

        // POST: Placements/SendEmployerForm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmployerForm(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id, tenantId);
            if (placement == null) return NotFound();

            var email = placement.Supervisor?.Email;
            var formToken = await _formTokenService.GenerateTokenAsync(id, "employer_acceptance", email, tenantId);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var formUrl = $"{baseUrl}/employer/form/{formToken.Token}";
            TempData["FormLink"] = formUrl;
            TempData["FormLinkMessage"] = "Employer form link generated. Copy and send to the employer:";

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Placements/DeleteEmployerFormToken
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        public async Task<IActionResult> ResendEmployerForm(Guid id, Guid tokenId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id, tenantId);
            if (placement == null) return NotFound();

            // Revoke the old token
            await _formTokenService.RevokeTokenByIdAsync(tokenId, tenantId);

            // Generate a new token
            var email = placement.Supervisor?.Email;
            var formToken = await _formTokenService.GenerateTokenAsync(id, "employer_acceptance", email, tenantId);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var formUrl = $"{baseUrl}/employer/form/{formToken.Token}";
            TempData["FormLink"] = formUrl;
            TempData["FormLinkMessage"] = "New employer form link generated (previous link revoked). Copy and send to the employer:";

            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Placements/EmployerFormReview/5
        public async Task<IActionResult> EmployerFormReview(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var placement = await _placementService.GetByIdWithDetailsAsync(id.Value, tenantId);
            if (placement == null) return NotFound();

            return View(placement);
        }

        private async Task PopulateDropdowns(Guid? studentId = null, Guid? companyId = null, Guid? supervisorId = null)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();

            var students = await _studentService.GetAllAsync(tenantId);
            ViewData["StudentId"] = new SelectList(
                students.Select(s => new { s.Id, FullName = $"{s.FirstName} {s.LastName}" }),
                "Id", "FullName", studentId);

            var companies = await _companyService.GetAllAsync(tenantId);
            ViewData["CompanyId"] = new SelectList(companies, "Id", "Name", companyId);

            var supervisors = await _supervisorService.GetAllAsync(tenantId);
            ViewData["SupervisorId"] = new SelectList(
                supervisors.Select(s => new { s.Id, FullName = $"{s.FirstName} {s.LastName}" }),
                "Id", "FullName", supervisorId);
        }
    }
}

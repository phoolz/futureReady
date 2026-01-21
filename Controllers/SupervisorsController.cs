using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FutureReady.Models.School;
using FutureReady.Services;
using FutureReady.Services.Supervisors;
using FutureReady.Services.Companies;

namespace FutureReady.Controllers
{
    [Authorize]
    public class SupervisorsController : Controller
    {
        private readonly ISupervisorService _supervisorService;
        private readonly ICompanyService _companyService;
        private readonly ITenantProvider? _tenantProvider;

        public SupervisorsController(
            ISupervisorService supervisorService,
            ICompanyService companyService,
            ITenantProvider? tenantProvider = null)
        {
            _supervisorService = supervisorService;
            _companyService = companyService;
            _tenantProvider = tenantProvider;
        }

        // GET: Supervisors
        public async Task<IActionResult> Index()
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var supervisors = await _supervisorService.GetAllAsync(tenantId);
            return View(supervisors);
        }

        // GET: Supervisors/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var supervisor = await _supervisorService.GetByIdAsync(id.Value, tenantId);
            if (supervisor == null) return NotFound();

            return View(supervisor);
        }

        // GET: Supervisors/Create
        public async Task<IActionResult> Create()
        {
            await PopulateCompaniesDropdown();
            return View();
        }

        // POST: Supervisors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CompanyId,FirstName,LastName,JobTitle,Email,Phone")] Supervisor supervisor)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCompaniesDropdown(supervisor.CompanyId);
                return View(supervisor);
            }

            try
            {
                await _supervisorService.CreateAsync(supervisor);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateCompaniesDropdown(supervisor.CompanyId);
                return View(supervisor);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Supervisors/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var supervisor = await _supervisorService.GetByIdAsync(id.Value, tenantId);
            if (supervisor == null) return NotFound();

            await PopulateCompaniesDropdown(supervisor.CompanyId);
            return View(supervisor);
        }

        // POST: Supervisors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,CompanyId,FirstName,LastName,JobTitle,Email,Phone,RowVersion")] Supervisor supervisor)
        {
            if (id != supervisor.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateCompaniesDropdown(supervisor.CompanyId);
                return View(supervisor);
            }

            try
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                await _supervisorService.UpdateAsync(supervisor, supervisor.RowVersion, tenantId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _supervisorService.ExistsAsync(id, _tenantProvider?.GetCurrentTenantId()))
                    return NotFound();
                else
                    throw;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await PopulateCompaniesDropdown(supervisor.CompanyId);
                return View(supervisor);
            }
        }

        // GET: Supervisors/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var supervisor = await _supervisorService.GetByIdAsync(id.Value, tenantId);
            if (supervisor == null) return NotFound();

            return View(supervisor);
        }

        // POST: Supervisors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            await _supervisorService.DeleteAsync(id, tenantId);
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateCompaniesDropdown(Guid? selectedCompanyId = null)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var companies = await _companyService.GetAllAsync(tenantId);
            ViewData["CompanyId"] = new SelectList(companies, "Id", "Name", selectedCompanyId);
        }
    }
}

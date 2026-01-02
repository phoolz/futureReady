using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using FutureReady.Data;
using FutureReady.Models.School;
using FutureReady.Services;
using FutureReady.Services.Cohorts;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace FutureReady.Controllers
{
    [Authorize]
    public class CohortsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;
        private readonly ICohortService _cohortService;

        public CohortsController(ApplicationDbContext context, ICohortService cohortService, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _cohortService = cohortService;
            _tenantProvider = tenantProvider;
        }

        // GET: SchoolCohorts
        public async Task<IActionResult> Index()
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var cohorts = await _cohortService.GetAllAsync(tenantId);
            return View(cohorts);
        }

        // GET: SchoolCohorts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var cohort = await _cohortService.GetByIdAsync(id.Value, tenantId);

            if (cohort == null) return NotFound();

            return View(cohort);
        }

        // GET: SchoolCohorts/Create
        public IActionResult Create()
        {
            // Show schools for the current tenant (the School.Id is treated as the tenant id)
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            // Find the school whose Id equals the current tenant id
            if (tenantId.HasValue)
            {
                var school = _context.Schools.AsNoTracking().FirstOrDefault(s => s.Id == tenantId.Value);
                ViewData["SchoolName"] = school?.Name;
                ViewData["SchoolId"] = school?.Id;
            }
            return View();
        }

        // POST: Cohorts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("SchoolId,GraduationYear,GraduationMonth")] Cohort cohort)
        {
            if (!ModelState.IsValid)
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                if (tenantId.HasValue)
                {
                    var school = _context.Schools.AsNoTracking().FirstOrDefault(s => s.Id == tenantId.Value);
                    ViewData["SchoolName"] = school?.Name;
                    ViewData["SchoolId"] = school?.Id;
                }
                return View(cohort);
            }

            // Ensure the cohort belongs to the current tenant (do not allow user to set TenantId manually)
            try
            {
                await _cohortService.CreateAsync(cohort);
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(string.Empty, "Unable to determine tenant. Please ensure you are operating within a tenant context.");
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                if (tenantId.HasValue)
                {
                    var school = _context.Schools.AsNoTracking().FirstOrDefault(s => s.Id == tenantId.Value);
                    ViewData["SchoolName"] = school?.Name;
                    ViewData["SchoolId"] = school?.Id;
                }
                return View(cohort);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Cohorts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var cohort = await _cohortService.GetByIdAsync(id.Value, tenantId);
            if (cohort == null) return NotFound();

            if (tenantId.HasValue)
            {
                var school = _context.Schools.AsNoTracking().FirstOrDefault(s => s.Id == tenantId.Value);
                ViewData["SchoolName"] = school?.Name;
                ViewData["SchoolId"] = school?.Id;
            }
            return View(cohort);
        }

        // POST: Cohorts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,SchoolId,GraduationYear,GraduationMonth,RowVersion")] Cohort cohort)
        {
            if (id != cohort.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                if (tenantId.HasValue)
                {
                    var school = _context.Schools.AsNoTracking().FirstOrDefault(s => s.Id == tenantId.Value);
                    ViewData["SchoolName"] = school?.Name;
                    ViewData["SchoolId"] = school?.Id;
                }
                return View(cohort);
            }

            try
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                await _cohortService.UpdateAsync(cohort, cohort.RowVersion, tenantId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _cohortService.ExistsAsync(id, _tenantProvider?.GetCurrentTenantId()))
                    return NotFound();
                else
                    throw;
            }
        }

        // GET: Cohorts/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var cohort = await _cohortService.GetByIdAsync(id.Value, tenantId);
            if (cohort == null) return NotFound();

            return View(cohort);
        }

        // POST: Cohorts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            await _cohortService.DeleteAsync(id, tenantId);
            return RedirectToAction(nameof(Index));
        }
    }
}

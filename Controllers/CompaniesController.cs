using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FutureReady.Models.School;
using FutureReady.Services;
using FutureReady.Services.Companies;

namespace FutureReady.Controllers
{
    [Authorize]
    public class CompaniesController : Controller
    {
        private readonly ICompanyService _companyService;
        private readonly ITenantProvider? _tenantProvider;

        public CompaniesController(ICompanyService companyService, ITenantProvider? tenantProvider = null)
        {
            _companyService = companyService;
            _tenantProvider = tenantProvider;
        }

        // GET: Companies
        public async Task<IActionResult> Index()
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var companies = await _companyService.GetAllAsync(tenantId);
            return View(companies);
        }

        // GET: Companies/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var company = await _companyService.GetByIdAsync(id.Value, tenantId);
            if (company == null) return NotFound();

            return View(company);
        }

        // GET: Companies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Companies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Industry,StreetAddress,StreetAddress2,Suburb,City,State,PostalCode,PostalStreetAddress,PostalSuburb,PostalCity,PostalState,PostalPostalCode,PublicLiabilityInsurance5M,InsuranceValue,HasPreviousWorkExperienceStudents")] Company company)
        {
            if (!ModelState.IsValid)
            {
                return View(company);
            }

            try
            {
                await _companyService.CreateAsync(company);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(company);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Companies/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var company = await _companyService.GetByIdAsync(id.Value, tenantId);
            if (company == null) return NotFound();

            return View(company);
        }

        // POST: Companies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Industry,StreetAddress,StreetAddress2,Suburb,City,State,PostalCode,PostalStreetAddress,PostalSuburb,PostalCity,PostalState,PostalPostalCode,PublicLiabilityInsurance5M,InsuranceValue,HasPreviousWorkExperienceStudents,RowVersion")] Company company)
        {
            if (id != company.Id) return NotFound();

            if (!ModelState.IsValid) return View(company);

            try
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                await _companyService.UpdateAsync(company, company.RowVersion, tenantId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _companyService.ExistsAsync(id, _tenantProvider?.GetCurrentTenantId()))
                    return NotFound();
                else
                    throw;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(company);
            }
        }

        // GET: Companies/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var company = await _companyService.GetByIdAsync(id.Value, tenantId);
            if (company == null) return NotFound();

            return View(company);
        }

        // POST: Companies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            await _companyService.DeleteAsync(id, tenantId);
            return RedirectToAction(nameof(Index));
        }
    }
}

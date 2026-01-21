using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.School;
using FutureReady.Services;
using FutureReady.Services.Schools;
using Microsoft.AspNetCore.Authorization;

namespace FutureReady.Controllers
{
    [Authorize]
    public class SchoolsController : Controller
    {
        private readonly ApplicationDbContext _context; // kept for now if other controllers/views depend on it
        private readonly ISchoolService _schoolService;

        public SchoolsController(ApplicationDbContext context, ISchoolService schoolService)
        {
            _context = context;
            _schoolService = schoolService;
        }

        // GET: Schools
        public async Task<IActionResult> Index()
        {
            var schools = await _schoolService.GetAllAsync();
            return View(schools);
        }

        // GET: Schools/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var school = await _schoolService.GetByIdAsync(id.Value);

            if (school == null) return NotFound();

            return View(school);
        }

        // GET: Schools/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Schools/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,TenantKey,Timezone,ContactEmail,ContactPhone")] School school)
        {
            if (!ModelState.IsValid) return View(school);

            await _schoolService.CreateAsync(school);
            return RedirectToAction(nameof(Index));
        }

        // GET: Schools/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var school = await _schoolService.GetByIdAsync(id.Value);
            if (school == null) return NotFound();
            return View(school);
        }

        // POST: Schools/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,TenantKey,Timezone,ContactEmail,ContactPhone,RowVersion")] School school)
        {
            if (id != school.Id) return NotFound();

            if (!ModelState.IsValid) return View(school);

            try
            {
                await _schoolService.UpdateAsync(school, school.RowVersion);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _schoolService.ExistsAsync(id))
                    return NotFound();
                else
                    throw;
            }
        }

        // GET: Schools/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var school = await _schoolService.GetByIdAsync(id.Value);
            if (school == null) return NotFound();

            return View(school);
        }

        // POST: Schools/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _schoolService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

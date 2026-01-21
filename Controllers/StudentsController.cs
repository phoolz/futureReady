using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using FutureReady.Data;
using FutureReady.Models.School;
using FutureReady.Services;
using FutureReady.Services.Students;
using FutureReady.Services.Placements;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace FutureReady.Controllers
{
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;
        private readonly IStudentService _studentService;
        private readonly IPlacementService _placementService;

        public StudentsController(ApplicationDbContext context, IStudentService studentService, IPlacementService placementService, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _studentService = studentService;
            _placementService = placementService;
            _tenantProvider = tenantProvider;
        }

        // GET: Students
        public async Task<IActionResult> Index()
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var students = await _studentService.GetAllAsync(tenantId);
            return View(students);
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var student = await _studentService.GetByIdAsync(id.Value, tenantId);
            if (student == null) return NotFound();

            // Get placements for this student
            var placements = await _placementService.GetByStudentIdAsync(id.Value, tenantId);
            ViewData["Placements"] = placements;

            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FirstName,LastName,PreferredName,DateOfBirth,StudentNumber,Phone,StudentType,YearLevel,GraduationYear,MedicareNumber")] Student student)
        {
            if (!ModelState.IsValid)
            {
                return View(student);
            }

            try
            {
                await _studentService.CreateAsync(student);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(student);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var student = await _studentService.GetByIdAsync(id.Value, tenantId);
            if (student == null) return NotFound();

            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FirstName,LastName,PreferredName,DateOfBirth,StudentNumber,Phone,StudentType,YearLevel,GraduationYear,MedicareNumber,RowVersion")] Student student)
        {
            if (id != student.Id) return NotFound();

            if (!ModelState.IsValid) return View(student);

            try
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                await _studentService.UpdateAsync(student, student.RowVersion, tenantId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _studentService.ExistsAsync(id, _tenantProvider?.GetCurrentTenantId()))
                    return NotFound();
                else
                    throw;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(student);
            }
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var student = await _studentService.GetByIdAsync(id.Value, tenantId);
            if (student == null) return NotFound();

            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            await _studentService.DeleteAsync(id, tenantId);
            return RedirectToAction(nameof(Index));
        }
    }
}

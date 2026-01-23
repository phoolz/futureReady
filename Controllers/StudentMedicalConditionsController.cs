using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FutureReady.Models;
using FutureReady.Models.School;
using FutureReady.Services;
using FutureReady.Services.StudentMedicalConditions;
using FutureReady.Services.Students;

namespace FutureReady.Controllers
{
    [Authorize(Roles = Roles.Teacher)]
    public class StudentMedicalConditionsController : Controller
    {
        private readonly IStudentMedicalConditionService _medicalConditionService;
        private readonly IStudentService _studentService;
        private readonly ITenantProvider? _tenantProvider;

        public StudentMedicalConditionsController(
            IStudentMedicalConditionService medicalConditionService,
            IStudentService studentService,
            ITenantProvider? tenantProvider = null)
        {
            _medicalConditionService = medicalConditionService;
            _studentService = studentService;
            _tenantProvider = tenantProvider;
        }

        // GET: StudentMedicalConditions/Index/5 (studentId)
        public async Task<IActionResult> Index(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var student = await _studentService.GetByIdAsync(id.Value, tenantId);
            if (student == null) return NotFound();

            var conditions = await _medicalConditionService.GetAllByStudentIdAsync(id.Value, tenantId);

            ViewData["Student"] = student;
            ViewData["StudentId"] = id.Value;
            return View(conditions);
        }

        // GET: StudentMedicalConditions/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var condition = await _medicalConditionService.GetByIdAsync(id.Value, tenantId);
            if (condition == null) return NotFound();

            return View(condition);
        }

        // GET: StudentMedicalConditions/Create?studentId=5
        public async Task<IActionResult> Create(Guid? studentId)
        {
            if (studentId == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var student = await _studentService.GetByIdAsync(studentId.Value, tenantId);
            if (student == null) return NotFound();

            ViewData["Student"] = student;
            ViewData["StudentId"] = studentId.Value;
            ViewData["ConditionTypes"] = MedicalConditionTypes.All;
            return View(new StudentMedicalCondition { StudentId = studentId.Value });
        }

        // POST: StudentMedicalConditions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentId,ConditionType,Details")] StudentMedicalCondition condition)
        {
            if (!ModelState.IsValid)
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                var student = await _studentService.GetByIdAsync(condition.StudentId, tenantId);
                ViewData["Student"] = student;
                ViewData["StudentId"] = condition.StudentId;
                ViewData["ConditionTypes"] = MedicalConditionTypes.All;
                return View(condition);
            }

            try
            {
                await _medicalConditionService.CreateAsync(condition);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                var student = await _studentService.GetByIdAsync(condition.StudentId, tenantId);
                ViewData["Student"] = student;
                ViewData["StudentId"] = condition.StudentId;
                ViewData["ConditionTypes"] = MedicalConditionTypes.All;
                return View(condition);
            }

            return RedirectToAction(nameof(Index), new { id = condition.StudentId });
        }

        // GET: StudentMedicalConditions/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var condition = await _medicalConditionService.GetByIdAsync(id.Value, tenantId);
            if (condition == null) return NotFound();

            ViewData["Student"] = condition.Student;
            ViewData["StudentId"] = condition.StudentId;
            ViewData["ConditionTypes"] = MedicalConditionTypes.All;
            return View(condition);
        }

        // POST: StudentMedicalConditions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,StudentId,ConditionType,Details,RowVersion")] StudentMedicalCondition condition)
        {
            if (id != condition.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                var student = await _studentService.GetByIdAsync(condition.StudentId, tenantId);
                ViewData["Student"] = student;
                ViewData["StudentId"] = condition.StudentId;
                ViewData["ConditionTypes"] = MedicalConditionTypes.All;
                return View(condition);
            }

            try
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                await _medicalConditionService.UpdateAsync(condition, condition.RowVersion, tenantId);
                return RedirectToAction(nameof(Index), new { id = condition.StudentId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _medicalConditionService.ExistsAsync(id, _tenantProvider?.GetCurrentTenantId()))
                    return NotFound();
                else
                    throw;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                var student = await _studentService.GetByIdAsync(condition.StudentId, tenantId);
                ViewData["Student"] = student;
                ViewData["StudentId"] = condition.StudentId;
                ViewData["ConditionTypes"] = MedicalConditionTypes.All;
                return View(condition);
            }
        }

        // GET: StudentMedicalConditions/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var condition = await _medicalConditionService.GetByIdAsync(id.Value, tenantId);
            if (condition == null) return NotFound();

            return View(condition);
        }

        // POST: StudentMedicalConditions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var condition = await _medicalConditionService.GetByIdAsync(id, tenantId);
            var studentId = condition?.StudentId;

            await _medicalConditionService.DeleteAsync(id, tenantId);

            if (studentId.HasValue)
                return RedirectToAction(nameof(Index), new { id = studentId.Value });

            return RedirectToAction("Index", "Students");
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FutureReady.Models;
using FutureReady.Models.School;
using FutureReady.Services;
using FutureReady.Services.EmergencyContacts;
using FutureReady.Services.Students;

namespace FutureReady.Controllers
{
    [Authorize(Roles = Roles.Teacher)]
    public class EmergencyContactsController : Controller
    {
        private readonly IEmergencyContactService _emergencyContactService;
        private readonly IStudentService _studentService;
        private readonly ITenantProvider? _tenantProvider;

        public EmergencyContactsController(
            IEmergencyContactService emergencyContactService,
            IStudentService studentService,
            ITenantProvider? tenantProvider = null)
        {
            _emergencyContactService = emergencyContactService;
            _studentService = studentService;
            _tenantProvider = tenantProvider;
        }

        // GET: EmergencyContacts/Index/5 (studentId)
        public async Task<IActionResult> Index(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var student = await _studentService.GetByIdAsync(id.Value, tenantId);
            if (student == null) return NotFound();

            var contacts = await _emergencyContactService.GetAllByStudentIdAsync(id.Value, tenantId);

            ViewData["Student"] = student;
            ViewData["StudentId"] = id.Value;
            return View(contacts);
        }

        // GET: EmergencyContacts/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var contact = await _emergencyContactService.GetByIdAsync(id.Value, tenantId);
            if (contact == null) return NotFound();

            return View(contact);
        }

        // GET: EmergencyContacts/Create?studentId=5
        public async Task<IActionResult> Create(Guid? studentId)
        {
            if (studentId == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var student = await _studentService.GetByIdAsync(studentId.Value, tenantId);
            if (student == null) return NotFound();

            ViewData["Student"] = student;
            ViewData["StudentId"] = studentId.Value;
            return View(new EmergencyContact { StudentId = studentId.Value });
        }

        // POST: EmergencyContacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StudentId,FirstName,LastName,MobileNumber,Relationship,IsPrimary")] EmergencyContact contact)
        {
            if (!ModelState.IsValid)
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                var student = await _studentService.GetByIdAsync(contact.StudentId, tenantId);
                ViewData["Student"] = student;
                ViewData["StudentId"] = contact.StudentId;
                return View(contact);
            }

            try
            {
                await _emergencyContactService.CreateAsync(contact);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                var student = await _studentService.GetByIdAsync(contact.StudentId, tenantId);
                ViewData["Student"] = student;
                ViewData["StudentId"] = contact.StudentId;
                return View(contact);
            }

            return RedirectToAction(nameof(Index), new { id = contact.StudentId });
        }

        // GET: EmergencyContacts/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var contact = await _emergencyContactService.GetByIdAsync(id.Value, tenantId);
            if (contact == null) return NotFound();

            ViewData["Student"] = contact.Student;
            ViewData["StudentId"] = contact.StudentId;
            return View(contact);
        }

        // POST: EmergencyContacts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,StudentId,FirstName,LastName,MobileNumber,Relationship,IsPrimary,RowVersion")] EmergencyContact contact)
        {
            if (id != contact.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                var student = await _studentService.GetByIdAsync(contact.StudentId, tenantId);
                ViewData["Student"] = student;
                ViewData["StudentId"] = contact.StudentId;
                return View(contact);
            }

            try
            {
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                await _emergencyContactService.UpdateAsync(contact, contact.RowVersion, tenantId);
                return RedirectToAction(nameof(Index), new { id = contact.StudentId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _emergencyContactService.ExistsAsync(id, _tenantProvider?.GetCurrentTenantId()))
                    return NotFound();
                else
                    throw;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var tenantId = _tenantProvider?.GetCurrentTenantId();
                var student = await _studentService.GetByIdAsync(contact.StudentId, tenantId);
                ViewData["Student"] = student;
                ViewData["StudentId"] = contact.StudentId;
                return View(contact);
            }
        }

        // GET: EmergencyContacts/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var contact = await _emergencyContactService.GetByIdAsync(id.Value, tenantId);
            if (contact == null) return NotFound();

            return View(contact);
        }

        // POST: EmergencyContacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var contact = await _emergencyContactService.GetByIdAsync(id, tenantId);
            var studentId = contact?.StudentId;

            await _emergencyContactService.DeleteAsync(id, tenantId);

            if (studentId.HasValue)
                return RedirectToAction(nameof(Index), new { id = studentId.Value });

            return RedirectToAction("Index", "Students");
        }
    }
}

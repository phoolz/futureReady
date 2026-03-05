using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Apiary.Data;
using Apiary.Models;
using Apiary.Models.School;
using Apiary.Services;
using Apiary.Services.Students;
using Apiary.Services.Placements;
using Apiary.Services.StudentAccountTokens;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace Apiary.Controllers
{
    [Authorize(Roles = Roles.Teacher)]
    public class StudentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITenantProvider? _tenantProvider;
        private readonly IStudentService _studentService;
        private readonly IPlacementService _placementService;
        private readonly IStudentAccountTokenService _accountTokenService;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentsController(ApplicationDbContext context, IStudentService studentService, IPlacementService placementService, IStudentAccountTokenService accountTokenService, UserManager<ApplicationUser> userManager, ITenantProvider? tenantProvider = null)
        {
            _context = context;
            _studentService = studentService;
            _placementService = placementService;
            _accountTokenService = accountTokenService;
            _userManager = userManager;
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

            // Get account tokens for this student
            var accountTokens = await _accountTokenService.GetByStudentIdAsync(id.Value, tenantId);
            ViewData["AccountTokens"] = accountTokens;

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
        public async Task<IActionResult> Create([Bind("FirstName,LastName,PreferredName,DateOfBirth,StudentNumber,Phone,Email,StudentType,YearLevel,GraduationYear,MedicareNumber")] Student student)
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

            // Get all users with Student role for linking
            var allUsers = await _userManager.Users.Where(u => !u.IsDeleted && u.TenantId == tenantId).ToListAsync();
            var studentUsers = new List<ApplicationUser>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(Roles.Student))
                {
                    studentUsers.Add(user);
                }
            }

            ViewData["StudentUsers"] = new SelectList(
                studentUsers.Select(u => new { u.Id, DisplayText = $"{u.DisplayName ?? u.UserName} ({u.Email})" }),
                "Id",
                "DisplayText",
                student.UserId
            );

            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,FirstName,LastName,PreferredName,DateOfBirth,StudentNumber,Phone,Email,StudentType,YearLevel,GraduationYear,MedicareNumber,UserId,RowVersion")] Student student)
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

        // POST: Students/GenerateActivationLink/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateActivationLink(Guid id)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            var student = await _studentService.GetByIdAsync(id, tenantId);
            if (student == null) return NotFound();

            if (string.IsNullOrWhiteSpace(student.Email))
            {
                TempData["ErrorMessage"] = "Student must have an email address to generate an activation link.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (student.UserId.HasValue)
            {
                TempData["ErrorMessage"] = "This student already has an account.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var token = await _accountTokenService.GenerateTokenAsync(id, tenantId);
                var activationLink = $"{Request.Scheme}://{Request.Host}/student/activate/{token.Token}";

                TempData["ActivationLink"] = activationLink;
                TempData["ActivationLinkMessage"] = $"Activation link generated for {student.Email}. Send this link to the student:";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Students/ResendActivationLink
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendActivationLink(Guid id, Guid tokenId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();

            // Revoke the old token
            await _accountTokenService.RevokeTokenByIdAsync(tokenId, tenantId);

            // Generate a new one
            return await GenerateActivationLink(id);
        }

        // POST: Students/DeleteActivationToken
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteActivationToken(Guid id, Guid tokenId)
        {
            var tenantId = _tenantProvider?.GetCurrentTenantId();
            await _accountTokenService.RevokeTokenByIdAsync(tokenId, tenantId);

            TempData["SuccessMessage"] = "Activation token has been deleted.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

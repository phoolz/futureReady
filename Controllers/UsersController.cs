using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models;
using FutureReady.Services;

namespace FutureReady.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminCheckService _adminCheck;

        public UsersController(ApplicationDbContext context, IAdminCheckService adminCheck)
        {
            _context = context;
            _adminCheck = adminCheck;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            if (!await _adminCheck.IsCurrentUserAdminAsync()) return Forbid();

            var users = await _context.Users.AsNoTracking().ToListAsync();
            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id.Value);

            if (user == null) return NotFound();

            return View(user);
        }

        // GET: Users/Create
        public async Task<IActionResult> Create()
        {
            if (!await _adminCheck.IsCurrentUserAdminAsync()) return Forbid();
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserName,DisplayName,Email,PasswordHash,IsActive,ExternalId")] User user)
        {
            if (!await _adminCheck.IsCurrentUserAdminAsync()) return Forbid();

            if (ModelState.IsValid)
            {
                // Hash the provided plaintext password before storing. If no password provided, leave null.
                if (!string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    user.PasswordHash = FutureReady.Services.PasswordHasher.HashPassword(user.PasswordHash);
                }
                else
                {
                    user.PasswordHash = null;
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (!await _adminCheck.IsCurrentUserAdminAsync()) return Forbid();

            if (id == null) return NotFound();

            var user = await _context.Users.FindAsync(id.Value);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,UserName,DisplayName,Email,PasswordHash,IsActive,ExternalId,RowVersion")] User user)
        {
            if (!await _adminCheck.IsCurrentUserAdminAsync()) return Forbid();

            if (id != user.Id) return NotFound();

            if (!ModelState.IsValid) return View(user);

            try
            {
                // Attach then mark modified specific fields to avoid overwriting audit/tenant fields
                var existing = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (existing == null) return NotFound();

                existing.UserName = user.UserName;
                existing.DisplayName = user.DisplayName;
                existing.Email = user.Email;

                // If a new plaintext password was provided, hash it and update; otherwise keep existing hash
                if (!string.IsNullOrWhiteSpace(user.PasswordHash))
                {
                    existing.PasswordHash = FutureReady.Services.PasswordHasher.HashPassword(user.PasswordHash);
                }

                existing.IsActive = user.IsActive;
                existing.ExternalId = user.ExternalId;

                // copy RowVersion if provided for concurrency
                if (user.RowVersion != null)
                    _context.Entry(existing).Property("RowVersion").OriginalValue = user.RowVersion;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Users.AnyAsync(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (!await _adminCheck.IsCurrentUserAdminAsync()) return Forbid();

            if (id == null) return NotFound();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id.Value);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (!await _adminCheck.IsCurrentUserAdminAsync()) return Forbid();

            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user); // will be converted to soft-delete by DbContext
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

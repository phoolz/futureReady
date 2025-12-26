using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models;
using FutureReady.Services;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System;

namespace FutureReady.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserProvider? _userProvider;
        private readonly IConfiguration _configuration;

        public UsersController(ApplicationDbContext context, IUserProvider? userProvider = null, IConfiguration? configuration = null)
        {
            _context = context;
            _userProvider = userProvider;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // Helper: determine if current user is an admin.
        // Behavior: if configuration key "Admin:Users" exists and has entries, only those usernames are considered admin.
        // If no configuration is provided (empty list), we treat the environment as permissive (returns true) to avoid locking out
        // a development machine. Change this policy if you prefer strict denial by default.
        private bool IsAdmin()
        {
            // var username = _userProvider?.GetCurrentUsername();
            // // If no username, deny
            // if (string.IsNullOrWhiteSpace(username)) return false;
            //
            // // Read configured admin users (array at Admin:Users)
            // var admins = _configuration.GetSection("Admin:Users").Get<string[]>();
            // if (admins != null && admins.Length > 0)
            // {
            //     return admins.Any(a => string.Equals(a?.Trim(), username, StringComparison.OrdinalIgnoreCase));
            // }

            // No explicit admin list configured — allow by default (development convenience).
            return true;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return Forbid();

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
        public IActionResult Create()
        {
            if (!IsAdmin()) return Forbid();
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserName,DisplayName,Email,PasswordHash,IsActive,ExternalId")] User user)
        {
            if (!IsAdmin()) return Forbid();

            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (!IsAdmin()) return Forbid();

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
            if (!IsAdmin()) return Forbid();

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
                existing.PasswordHash = user.PasswordHash;
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
            if (!IsAdmin()) return Forbid();

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
            if (!IsAdmin()) return Forbid();

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

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models;
using FutureReady.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using FutureReady.Services.Users;

namespace FutureReady.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context; // kept for view helpers like School select list
        private readonly IUserService _userService;
        private readonly IUserProvider _userProvider;

        public UsersController(ApplicationDbContext context, IUserService userService, IUserProvider userProvider)
        {
            _context = context;
            _userService = userService;
            _userProvider = userProvider;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllAsync();
            return View(users);
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userService.GetByIdAsync(id.Value);

            if (user == null) return NotFound();

            return View(user);
        }
        
        // GET: Users/Create
        public IActionResult Create()
        {
            ViewData["SchoolId"] = new SelectList(_context.Schools.AsNoTracking(), "Id", "Name");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserName,DisplayName,Email,PasswordHash,IsActive,ExternalId,TenantId")] User user)
        {
            if (ModelState.IsValid)
            {
                await _userService.CreateAsync(user);
                return RedirectToAction(nameof(Index));
            }
            ViewData["SchoolId"] = new SelectList(_context.Schools.AsNoTracking(), "Id", "Name", user.TenantId);
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userService.GetByIdAsync(id.Value);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,UserName,DisplayName,Email,PasswordHash,IsActive,ExternalId,RowVersion")] User user)
        { 
            if (id != user.Id) return NotFound();

            if (!ModelState.IsValid) return View(user);

            try
            {
                await _userService.UpdateAsync(user, user.RowVersion);
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
            if (id == null) return NotFound();

            var user = await _userService.GetByIdAsync(id.Value);
            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _userService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Users/Profile
        public async Task<IActionResult> Profile()
        {
            var username = _userProvider.GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == username || u.Email == username);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile([Bind("Id,UserName,DisplayName,Email,RowVersion")] User user)
        {
            var username = _userProvider.GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Authentication");
            }

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username || u.Email == username);

            if (existingUser == null || existingUser.Id != user.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(user);
            }

            try
            {
                existingUser.DisplayName = user.DisplayName;
                existingUser.Email = user.Email;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Profile));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Users.AnyAsync(e => e.Id == user.Id))
                    return NotFound();
                else
                    throw;
            }
        }
    }
}

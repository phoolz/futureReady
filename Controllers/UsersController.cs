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

        public UsersController(ApplicationDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;
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
    }
}

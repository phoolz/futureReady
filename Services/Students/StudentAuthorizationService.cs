using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using FutureReady.Models;

namespace FutureReady.Services.Students
{
    public class StudentAuthorizationService : IStudentAuthorizationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserProvider _userProvider;
        private readonly ITenantProvider _tenantProvider;
        private readonly IStudentService _studentService;

        public StudentAuthorizationService(
            UserManager<ApplicationUser> userManager,
            IUserProvider userProvider,
            ITenantProvider tenantProvider,
            IStudentService studentService)
        {
            _userManager = userManager;
            _userProvider = userProvider;
            _tenantProvider = tenantProvider;
            _studentService = studentService;
        }

        public async Task<bool> CanAccessStudentDataAsync(Guid studentId)
        {
            var username = _userProvider.GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
            {
                return false;
            }

            var user = await _userManager.FindByNameAsync(username)
                       ?? await _userManager.FindByEmailAsync(username);

            if (user == null)
            {
                return false;
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Site Admins cannot access student data
            if (roles.Contains(Roles.SiteAdmin))
            {
                return false;
            }

            // Teachers can access students in their tenant
            if (roles.Contains(Roles.Teacher))
            {
                var tenantId = _tenantProvider.GetCurrentTenantId();
                var student = await _studentService.GetByIdAsync(studentId, tenantId);
                return student != null;
            }

            // Students can only access their own data
            if (roles.Contains(Roles.Student))
            {
                var student = await _studentService.GetByIdAsync(studentId, null);
                return student?.UserId == user.Id;
            }

            return false;
        }

        public async Task<Guid?> GetCurrentUserStudentIdAsync()
        {
            var username = _userProvider.GetCurrentUsername();
            if (string.IsNullOrEmpty(username))
            {
                return null;
            }

            var user = await _userManager.FindByNameAsync(username)
                       ?? await _userManager.FindByEmailAsync(username);

            if (user == null)
            {
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);

            // Only students have a linked student record
            if (!roles.Contains(Roles.Student))
            {
                return null;
            }

            // Find the student record linked to this user
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var students = await _studentService.GetAllAsync(tenantId);
            var student = students.FirstOrDefault(s => s.UserId == user.Id);

            return student?.Id;
        }
    }
}

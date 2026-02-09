using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models;
using FutureReady.Models.StudentActivation;
using FutureReady.Services.StudentAccountTokens;

namespace FutureReady.Services.StudentActivation
{
    public class StudentActivationService : IStudentActivationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStudentAccountTokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentActivationService(
            ApplicationDbContext context,
            IStudentAccountTokenService tokenService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _tokenService = tokenService;
            _userManager = userManager;
        }

        public async Task<StudentActivationDto?> InitializeFormAsync(string token)
        {
            var accountToken = await _tokenService.ValidateTokenAsync(token);

            if (accountToken == null || !accountToken.IsValid)
                return null;

            var student = accountToken.Student;
            if (student == null)
                return null;

            // Student already has an account
            if (student.UserId.HasValue)
                return null;

            return new StudentActivationDto
            {
                StudentId = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email ?? string.Empty,
                PreferredName = student.PreferredName,
                DateOfBirth = student.DateOfBirth,
                StudentNumber = student.StudentNumber,
                Phone = student.Phone,
                StudentType = student.StudentType,
                YearLevel = student.YearLevel,
                GraduationYear = student.GraduationYear,
                MedicareNumber = student.MedicareNumber
            };
        }

        public async Task<(bool Success, string? ErrorMessage)> ActivateAccountAsync(string token, StudentActivationDto dto)
        {
            var accountToken = await _tokenService.ValidateTokenAsync(token);

            if (accountToken == null || !accountToken.IsValid)
                return (false, "This activation link is no longer valid. Please request a new link from your school.");

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == accountToken.StudentId);

            if (student == null)
                return (false, "Student record not found.");

            // Check if student already has an account
            if (student.UserId.HasValue)
                return (false, "An account has already been created for this student.");

            // Check if email is already in use by another user
            var existingUser = await _userManager.FindByEmailAsync(student.Email!);
            if (existingUser != null)
                return (false, "An account with this email address already exists. Please contact your school for assistance.");

            // Validate passwords match
            if (dto.Password != dto.ConfirmPassword)
                return (false, "Passwords do not match.");

            // Create the user
            var user = new ApplicationUser
            {
                UserName = student.Email,
                Email = student.Email,
                DisplayName = $"{student.FirstName} {student.LastName}",
                TenantId = accountToken.TenantId,
                IsActive = true,
                EmailConfirmed = true // Auto-confirm since they came from a magic link
            };

            var createResult = await _userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                return (false, $"Failed to create account: {errors}");
            }

            // Add student role
            var roleResult = await _userManager.AddToRoleAsync(user, Roles.Student);
            if (!roleResult.Succeeded)
            {
                // Clean up user if role assignment fails
                await _userManager.DeleteAsync(user);
                return (false, "Failed to assign student role. Please try again.");
            }

            // Update student record with user link and profile info
            student.UserId = user.Id;
            student.PreferredName = dto.PreferredName;
            student.DateOfBirth = dto.DateOfBirth;
            student.StudentNumber = dto.StudentNumber;
            student.Phone = dto.Phone;
            student.StudentType = dto.StudentType;
            student.YearLevel = dto.YearLevel;
            student.GraduationYear = dto.GraduationYear;
            student.MedicareNumber = dto.MedicareNumber;

            await _context.SaveChangesAsync();

            // Mark token as used
            await _tokenService.MarkAsUsedAsync(token);

            return (true, null);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Apiary.Models;
using Apiary.Models.School;
using Apiary.Models.School.Enums;

namespace Apiary.Data
{
    /// <summary>
    /// Seeds test data for development and testing. Only runs in Development environment.
    /// </summary>
    public class TestDataSeeder
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger _logger;

        // Track created entities for relationships
        private readonly List<School> _schools = [];
        private readonly List<ApplicationUser> _teacherUsers = [];
        private readonly List<ApplicationUser> _studentUsers = [];
        private readonly List<Teacher> _teachers = [];
        private readonly List<Student> _students = [];
        private readonly List<Company> _companies = [];
        private readonly List<Supervisor> _supervisors = [];
        private readonly List<Placement> _placements = [];
        private readonly List<PlacementStudent> _placementStudents = [];

        public TestDataSeeder(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ILogger logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            _logger.LogInformation("Starting test data seeding...");

            await ClearExistingTestDataAsync();

            await SeedSchoolsAsync();
            await SeedUsersAsync();
            await SeedTeachersAsync();
            await SeedStudentsAsync();
            await SeedEmergencyContactsAsync();
            await SeedMedicalConditionsAsync();
            await SeedCompaniesAsync();
            await SeedSupervisorsAsync();
            await SeedPlacementsAsync();
            await SeedPlacementStudentsAsync();
            await SeedParentPermissionsAsync();
            await SeedFormTokensAsync();
            await SeedStudentAccountTokensAsync();
            await SeedLogbookEntriesAsync();
            await SeedLogbookTasksAsync();
            await SeedLogbookEvaluationsAsync();

            _logger.LogInformation("Test data seeding completed successfully.");
        }

        private async Task ClearExistingTestDataAsync()
        {
            _logger.LogInformation("Clearing existing test data...");

            // Get Admin school to preserve
            var adminSchool = await _db.Schools.IgnoreQueryFilters()
                .FirstOrDefaultAsync(s => s.Name == "Admin");
            var adminSchoolId = adminSchool?.Id ?? Guid.Empty;

            // Get admin user to preserve
            var adminUser = await _userManager.Users.IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.UserName == "adminsean");
            var adminUserId = adminUser?.Id ?? Guid.Empty;

            // Delete in reverse dependency order using ExecuteDeleteAsync for hard deletes
            // This bypasses the soft-delete interception in SaveChangesAsync

            // LogbookEvaluations
            await _db.LogbookEvaluations.IgnoreQueryFilters()
                .Where(e => e.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // LogbookTasks
            await _db.LogbookTasks.IgnoreQueryFilters()
                .Where(t => t.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // LogbookEntries
            await _db.LogbookEntries.IgnoreQueryFilters()
                .Where(e => e.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // StudentAccountTokens
            await _db.StudentAccountTokens.IgnoreQueryFilters()
                .Where(t => t.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // FormTokens
            await _db.FormTokens.IgnoreQueryFilters()
                .Where(t => t.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // ParentPermissions
            await _db.ParentPermissions.IgnoreQueryFilters()
                .Where(p => p.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // PlacementStudents
            await _db.PlacementStudents.IgnoreQueryFilters()
                .Where(ps => ps.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // Placements
            await _db.Placements.IgnoreQueryFilters()
                .Where(p => p.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // Supervisors
            await _db.Supervisors.IgnoreQueryFilters()
                .Where(s => s.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // Companies
            await _db.Companies.IgnoreQueryFilters()
                .Where(c => c.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // StudentMedicalConditions
            await _db.StudentMedicalConditions.IgnoreQueryFilters()
                .Where(c => c.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // EmergencyContacts
            await _db.EmergencyContacts.IgnoreQueryFilters()
                .Where(c => c.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // Students
            await _db.Students.IgnoreQueryFilters()
                .Where(s => s.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // Teachers
            await _db.Teachers.IgnoreQueryFilters()
                .Where(t => t.TenantId != adminSchoolId)
                .ExecuteDeleteAsync();

            // Users (except admin) - need to delete related AspNetUserRoles first
            var usersToDelete = await _userManager.Users.IgnoreQueryFilters()
                .Where(u => u.Id != adminUserId && u.TenantId != adminSchoolId)
                .Select(u => u.Id)
                .ToListAsync();

            if (usersToDelete.Count > 0)
            {
                // Delete user roles via raw SQL to avoid EF tracking issues
                await _db.Database.ExecuteSqlRawAsync(
                    "DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM AspNetUsers WHERE Id != {0} AND TenantId != {1})",
                    adminUserId, adminSchoolId);

                // Delete users
                await _db.Users.IgnoreQueryFilters()
                    .Where(u => u.Id != adminUserId && u.TenantId != adminSchoolId)
                    .ExecuteDeleteAsync();
            }

            // Schools (except Admin)
            await _db.Schools.IgnoreQueryFilters()
                .Where(s => s.Name != "Admin")
                .ExecuteDeleteAsync();

            _logger.LogInformation("Existing test data cleared.");
        }

        private async Task SeedSchoolsAsync()
        {
            _logger.LogInformation("Seeding schools...");

            var schoolData = new[]
            {
                ("Northside Secondary College", "northside"),
                ("Eastview High School", "eastview")
            };

            foreach (var (name, tenantKey) in schoolData)
            {
                var school = new School
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    TenantKey = tenantKey,
                    Timezone = "Australia/Melbourne",
                    ContactEmail = $"info@{tenantKey}.edu.au",
                    ContactPhone = TestDataGenerator.GetLandlinePhone(),
                    CreatedBy = "TestDataSeeder"
                };
                _db.Schools.Add(school);
                _schools.Add(school);
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} schools.", _schools.Count);
        }

        private async Task SeedUsersAsync()
        {
            _logger.LogInformation("Seeding users...");

            var teacherNumber = 1;
            var studentNumber = 1;

            foreach (var school in _schools)
            {
                // Create 2 teacher users per school
                for (int i = 1; i <= 2; i++)
                {
                    var firstName = TestDataGenerator.GetFirstName();
                    var lastName = TestDataGenerator.GetLastName();
                    var email = $"teacher{teacherNumber}@test.com";
                    var user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = email,
                        Email = email,
                        DisplayName = $"{firstName} {lastName}",
                        TenantId = school.Id,
                        IsActive = true,
                        CreatedBy = "TestDataSeeder"
                    };

                    var result = await _userManager.CreateAsync(user, "TestPassword123!");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, Roles.Teacher);
                        _teacherUsers.Add(user);
                        teacherNumber++;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create teacher user {UserName}: {Errors}",
                            user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }

                // Create 2 student users per school
                for (int i = 1; i <= 2; i++)
                {
                    var firstName = TestDataGenerator.GetFirstName();
                    var lastName = TestDataGenerator.GetLastName();
                    var email = $"student{studentNumber}@test.com";
                    var user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = email,
                        Email = email,
                        DisplayName = $"{firstName} {lastName}",
                        TenantId = school.Id,
                        IsActive = true,
                        CreatedBy = "TestDataSeeder"
                    };

                    var result = await _userManager.CreateAsync(user, "TestPassword123!");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, Roles.Student);
                        _studentUsers.Add(user);
                        studentNumber++;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create student user {UserName}: {Errors}",
                            user.UserName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            _logger.LogInformation("Seeded {Count} teacher users and {StudentCount} student users.",
                _teacherUsers.Count, _studentUsers.Count);
        }

        private async Task SeedTeachersAsync()
        {
            _logger.LogInformation("Seeding teachers...");

            var userIndex = 0;
            foreach (var school in _schools)
            {
                var schoolTeacherUsers = _teacherUsers.Where(u => u.TenantId == school.Id).ToList();

                foreach (var user in schoolTeacherUsers)
                {
                    var nameParts = user.DisplayName?.Split(' ') ?? ["Test", "Teacher"];
                    var teacher = new Teacher
                    {
                        Id = Guid.NewGuid(),
                        TenantId = school.Id,
                        UserId = user.Id,
                        SchoolId = school.Id,
                        FirstName = nameParts[0],
                        LastName = nameParts.Length > 1 ? nameParts[1] : "Teacher",
                        Phone = TestDataGenerator.GetAustralianPhone(),
                        Title = userIndex % 2 == 0 ? "Head of Careers" : "Work Experience Coordinator",
                        HireDate = DateTimeOffset.UtcNow.AddYears(-TestDataGenerator.GetRandomInt(1, 10)),
                        IsActive = true,
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.Teachers.Add(teacher);
                    _teachers.Add(teacher);
                    userIndex++;
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} teachers.", _teachers.Count);
        }

        private async Task SeedStudentsAsync()
        {
            _logger.LogInformation("Seeding students...");

            var studentIndex = 1;
            foreach (var school in _schools)
            {
                // Get student users for this school (2 per school)
                var schoolStudentUsers = _studentUsers.Where(u => u.TenantId == school.Id).ToList();

                // 10 students per school
                for (int i = 0; i < 10; i++)
                {
                    // First 2 students are linked to user accounts
                    ApplicationUser? linkedUser = i < schoolStudentUsers.Count ? schoolStudentUsers[i] : null;

                    string firstName, lastName;
                    if (linkedUser != null)
                    {
                        // Use the user's name for linked students
                        var nameParts = linkedUser.DisplayName?.Split(' ') ?? ["Test", "Student"];
                        firstName = nameParts[0];
                        lastName = nameParts.Length > 1 ? nameParts[1] : "Student";
                    }
                    else
                    {
                        firstName = TestDataGenerator.GetFirstName();
                        lastName = TestDataGenerator.GetLastName();
                    }

                    var student = new Student
                    {
                        Id = Guid.NewGuid(),
                        TenantId = school.Id,
                        UserId = linkedUser?.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        PreferredName = TestDataGenerator.GetRandomBool(0.3) ? TestDataGenerator.GetFirstName() : null,
                        DateOfBirth = TestDataGenerator.GetBirthDate(),
                        StudentNumber = TestDataGenerator.GetStudentNumber(studentIndex),
                        Phone = TestDataGenerator.GetRandomBool(0.8) ? TestDataGenerator.GetAustralianPhone() : null,
                        Email = linkedUser?.Email ?? $"{firstName.ToLower()}.{lastName.ToLower()}@student.{school.TenantKey}.edu.au",
                        StudentType = "VCE",
                        YearLevel = TestDataGenerator.GetRandom(new[] { "10", "11", "12" }),
                        GraduationYear = DateTime.Now.Year + TestDataGenerator.GetRandomInt(0, 2),
                        MedicareNumber = TestDataGenerator.GetRandomBool(0.7) ? TestDataGenerator.GetMedicareNumber() : null,
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.Students.Add(student);
                    _students.Add(student);
                    studentIndex++;
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} students.", _students.Count);
        }

        private async Task SeedEmergencyContactsAsync()
        {
            _logger.LogInformation("Seeding emergency contacts...");

            var contactCount = 0;
            foreach (var student in _students)
            {
                // 1-2 contacts per student
                var numContacts = TestDataGenerator.GetRandomInt(1, 2);
                for (int i = 0; i < numContacts; i++)
                {
                    var contact = new EmergencyContact
                    {
                        Id = Guid.NewGuid(),
                        TenantId = student.TenantId,
                        StudentId = student.Id,
                        FirstName = TestDataGenerator.GetFirstName(),
                        LastName = i == 0 ? student.LastName : TestDataGenerator.GetLastName(),
                        MobileNumber = TestDataGenerator.GetAustralianPhone(),
                        Relationship = TestDataGenerator.GetRelationship(),
                        IsPrimary = i == 0,
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.EmergencyContacts.Add(contact);
                    contactCount++;
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} emergency contacts.", contactCount);
        }

        private async Task SeedMedicalConditionsAsync()
        {
            _logger.LogInformation("Seeding medical conditions...");

            var conditionCount = 0;
            foreach (var student in _students)
            {
                // 0-2 conditions per student (30% chance of having any)
                if (!TestDataGenerator.GetRandomBool(0.3)) continue;

                var numConditions = TestDataGenerator.GetRandomInt(1, 2);
                var usedTypes = new HashSet<string>();

                for (int i = 0; i < numConditions; i++)
                {
                    string conditionType;
                    do
                    {
                        conditionType = TestDataGenerator.GetRandom(MedicalConditionTypes.All);
                    } while (usedTypes.Contains(conditionType));
                    usedTypes.Add(conditionType);

                    var condition = new StudentMedicalCondition
                    {
                        Id = Guid.NewGuid(),
                        TenantId = student.TenantId,
                        StudentId = student.Id,
                        ConditionType = conditionType,
                        Details = TestDataGenerator.GetMedicalConditionDetails(),
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.StudentMedicalConditions.Add(condition);
                    conditionCount++;
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} medical conditions.", conditionCount);
        }

        private async Task SeedCompaniesAsync()
        {
            _logger.LogInformation("Seeding companies...");

            foreach (var school in _schools)
            {
                // 5 companies per school
                for (int i = 0; i < 5; i++)
                {
                    var company = new Company
                    {
                        Id = Guid.NewGuid(),
                        TenantId = school.Id,
                        Name = TestDataGenerator.GetCompanyName(),
                        Industry = TestDataGenerator.GetIndustry(),
                        StreetAddress = TestDataGenerator.GetStreetAddress(),
                        Suburb = TestDataGenerator.GetSuburb(),
                        State = "VIC",
                        PostalCode = TestDataGenerator.GetPostalCode(),
                        PublicLiabilityInsurance5M = TestDataGenerator.GetRandomBool(0.8),
                        InsuranceValue = TestDataGenerator.GetRandomBool(0.8) ? "$10,000,000" : "$5,000,000",
                        HasPreviousWorkExperienceStudents = TestDataGenerator.GetRandomBool(0.6),
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.Companies.Add(company);
                    _companies.Add(company);
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} companies.", _companies.Count);
        }

        private async Task SeedSupervisorsAsync()
        {
            _logger.LogInformation("Seeding supervisors...");

            foreach (var company in _companies)
            {
                // 1-2 supervisors per company
                var numSupervisors = TestDataGenerator.GetRandomInt(1, 2);
                for (int i = 0; i < numSupervisors; i++)
                {
                    var firstName = TestDataGenerator.GetFirstName();
                    var lastName = TestDataGenerator.GetLastName();
                    var supervisor = new Supervisor
                    {
                        Id = Guid.NewGuid(),
                        TenantId = company.TenantId,
                        CompanyId = company.Id,
                        FirstName = firstName,
                        LastName = lastName,
                        JobTitle = TestDataGenerator.GetJobTitle(),
                        Email = TestDataGenerator.GetEmail(firstName, lastName),
                        Phone = TestDataGenerator.GetAustralianPhone(),
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.Supervisors.Add(supervisor);
                    _supervisors.Add(supervisor);
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} supervisors.", _supervisors.Count);
        }

        private async Task SeedPlacementsAsync()
        {
            _logger.LogInformation("Seeding placements...");

            var statuses = new[] { "draft", "pending_employer", "confirmed" };

            foreach (var school in _schools)
            {
                var schoolCompanies = _companies.Where(c => c.TenantId == school.Id).ToList();
                var schoolSupervisors = _supervisors.Where(s => s.TenantId == school.Id).ToList();

                // 3 placements per school
                for (int i = 0; i < 3; i++)
                {
                    var company = schoolCompanies[i % schoolCompanies.Count];
                    var supervisor = schoolSupervisors.FirstOrDefault(s => s.CompanyId == company.Id);
                    var status = statuses[i % statuses.Length];
                    var startDate = TestDataGenerator.GetPlacementStartDate();

                    var placement = new Placement
                    {
                        Id = Guid.NewGuid(),
                        TenantId = school.Id,
                        CompanyId = company.Id,
                        SupervisorId = supervisor?.Id,
                        Year = DateTime.Now.Year,
                        StartDate = startDate,
                        FinishDate = startDate.AddDays(TestDataGenerator.GetRandomInt(5, 10)),
                        Status = status,
                        PlacementRole = TestDataGenerator.GetPlacementRole(),
                        DressRequirement = "Business casual attire",
                        WorkStartTime = TestDataGenerator.GetWorkTime(true),
                        WorkEndTime = TestDataGenerator.GetWorkTime(false),
                        HasOhsPolicy = true,
                        HasInductionProgram = true,
                        SafetyBriefingMethod = "On-site induction with supervisor",
                        HasObviousHazards = TestDataGenerator.GetRandomBool(0.3),
                        HazardDetails = TestDataGenerator.GetRandomBool(0.3) ? "Standard office environment hazards" : null,
                        ProvidesHazardReportingInstruction = true,
                        HasEmergencyProcedures = true,
                        HasFireExtinguishersChecked = true,
                        HasFirstAidKit = true,
                        HasSafeAmenities = true,
                        StaffInformedOfStudent = true,
                        StaffMeetWorkingWithChildrenRequirements = true,
                        EmployerRequiresVehicleTravel = TestDataGenerator.GetRandomBool(0.2),
                        EmployerSubmittedAt = status != "draft" ? DateTime.UtcNow.AddDays(-TestDataGenerator.GetRandomInt(1, 14)) : null,
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.Placements.Add(placement);
                    _placements.Add(placement);
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} placements.", _placements.Count);
        }

        private async Task SeedPlacementStudentsAsync()
        {
            _logger.LogInformation("Seeding placement students...");

            foreach (var placement in _placements)
            {
                var schoolStudents = _students.Where(s => s.TenantId == placement.TenantId).ToList();
                var usedStudents = new HashSet<Guid>();

                // 1-3 students per placement
                var numStudents = TestDataGenerator.GetRandomInt(1, 3);
                for (int i = 0; i < numStudents && i < schoolStudents.Count; i++)
                {
                    var student = schoolStudents.FirstOrDefault(s => !usedStudents.Contains(s.Id));
                    if (student == null) break;

                    usedStudents.Add(student.Id);

                    var psStatus = placement.Status == "confirmed" ? "confirmed" : "pending_parent";

                    var placementStudent = new PlacementStudent
                    {
                        Id = Guid.NewGuid(),
                        TenantId = placement.TenantId,
                        PlacementId = placement.Id,
                        StudentId = student.Id,
                        Status = psStatus,
                        ParentSubmittedAt = psStatus == "confirmed" ? DateTime.UtcNow.AddDays(-TestDataGenerator.GetRandomInt(1, 7)) : null,
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.PlacementStudents.Add(placementStudent);
                    _placementStudents.Add(placementStudent);
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} placement students.", _placementStudents.Count);
        }

        private async Task SeedParentPermissionsAsync()
        {
            _logger.LogInformation("Seeding parent permissions...");

            var permissionCount = 0;
            var confirmedPlacementStudents = _placementStudents.Where(ps => ps.Status == "confirmed").ToList();

            foreach (var ps in confirmedPlacementStudents)
            {
                var student = _students.First(s => s.Id == ps.StudentId);
                var parentFirstName = TestDataGenerator.GetFirstName();

                var permission = new ParentPermission
                {
                    Id = Guid.NewGuid(),
                    TenantId = ps.TenantId,
                    PlacementId = ps.PlacementId,
                    StudentId = ps.StudentId,
                    TransportMethod = TestDataGenerator.GetRandom(new[] { "public", "private_car", "combination" }),
                    PublicTransportDetails = "Bus and train to workplace",
                    DriverName = $"{parentFirstName} {student.LastName}",
                    DriverContactNumber = TestDataGenerator.GetAustralianPhone(),
                    RequestTeacherPrevisit = TestDataGenerator.GetRandomBool(0.2),
                    ShareMedicalWithEmployer = TestDataGenerator.GetRandomBool(0.4),
                    ParentFirstName = parentFirstName,
                    ParentLastName = student.LastName,
                    ConsentGiven = true,
                    ConsentDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-TestDataGenerator.GetRandomInt(1, 14))),
                    CreatedBy = "TestDataSeeder"
                };
                _db.ParentPermissions.Add(permission);
                permissionCount++;
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} parent permissions.", permissionCount);
        }

        private async Task SeedFormTokensAsync()
        {
            _logger.LogInformation("Seeding form tokens...");

            var tokenCount = 0;
            foreach (var placement in _placements)
            {
                // Employer form token (one per placement)
                var employerToken = new FormToken
                {
                    Id = Guid.NewGuid(),
                    TenantId = placement.TenantId,
                    PlacementId = placement.Id,
                    StudentId = null, // Employer tokens are placement-wide
                    Token = TestDataGenerator.GenerateToken(),
                    FormType = "employer_acceptance",
                    Email = _supervisors.FirstOrDefault(s => s.Id == placement.SupervisorId)?.Email,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    UsedAt = placement.Status != "draft" ? DateTime.UtcNow.AddDays(-TestDataGenerator.GetRandomInt(1, 14)) : null,
                    CreatedBy = "TestDataSeeder"
                };
                _db.FormTokens.Add(employerToken);
                tokenCount++;

                // Parent form tokens (one per student in placement)
                var placementStudents = _placementStudents.Where(ps => ps.PlacementId == placement.Id).ToList();
                foreach (var ps in placementStudents)
                {
                    var student = _students.First(s => s.Id == ps.StudentId);
                    var contact = await _db.EmergencyContacts
                        .FirstOrDefaultAsync(c => c.StudentId == student.Id && c.IsPrimary);

                    var parentToken = new FormToken
                    {
                        Id = Guid.NewGuid(),
                        TenantId = placement.TenantId,
                        PlacementId = placement.Id,
                        StudentId = student.Id,
                        Token = TestDataGenerator.GenerateToken(),
                        FormType = "parent_permission",
                        Email = contact != null ? $"{contact.FirstName.ToLower()}.{contact.LastName.ToLower()}@example.com" : null,
                        ExpiresAt = DateTime.UtcNow.AddDays(30),
                        UsedAt = ps.Status == "confirmed" ? DateTime.UtcNow.AddDays(-TestDataGenerator.GetRandomInt(1, 7)) : null,
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.FormTokens.Add(parentToken);
                    tokenCount++;
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} form tokens.", tokenCount);
        }

        private async Task SeedStudentAccountTokensAsync()
        {
            _logger.LogInformation("Seeding student account tokens...");

            var tokenCount = 0;
            foreach (var school in _schools)
            {
                var schoolStudents = _students.Where(s => s.TenantId == school.Id).Take(3).ToList();

                foreach (var student in schoolStudents)
                {
                    var token = new StudentAccountToken
                    {
                        Id = Guid.NewGuid(),
                        TenantId = school.Id,
                        StudentId = student.Id,
                        Token = TestDataGenerator.GenerateToken(),
                        ExpiresAt = DateTime.UtcNow.AddDays(7),
                        UsedAt = null,
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.StudentAccountTokens.Add(token);
                    tokenCount++;
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} student account tokens.", tokenCount);
        }

        private async Task SeedLogbookEntriesAsync()
        {
            _logger.LogInformation("Seeding logbook entries...");

            var entryCount = 0;
            var confirmedPlacementStudents = _placementStudents
                .Where(ps => ps.Status == "confirmed")
                .ToList();

            foreach (var ps in confirmedPlacementStudents)
            {
                var placement = _placements.First(p => p.Id == ps.PlacementId);
                if (!placement.StartDate.HasValue) continue;

                // 5-10 entries per confirmed placement student
                var numEntries = TestDataGenerator.GetRandomInt(5, 10);
                var currentDate = placement.StartDate.Value;

                for (int i = 0; i < numEntries; i++)
                {
                    // Skip weekends
                    while (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        currentDate = currentDate.AddDays(1);
                    }

                    var (start, lunchStart, lunchEnd, finish, hours) = TestDataGenerator.GetLogbookTimes();

                    var entry = new LogbookEntry
                    {
                        Id = Guid.NewGuid(),
                        TenantId = ps.TenantId,
                        PlacementStudentId = ps.Id,
                        Date = currentDate,
                        StartTime = start,
                        LunchStartTime = lunchStart,
                        LunchEndTime = lunchEnd,
                        FinishTime = finish,
                        TotalHoursWorked = hours,
                        SupervisorVerified = TestDataGenerator.GetRandomBool(0.7),
                        SupervisorVerifiedAt = TestDataGenerator.GetRandomBool(0.7) ? DateTimeOffset.UtcNow.AddDays(-TestDataGenerator.GetRandomInt(0, 5)) : null,
                        CreatedBy = "TestDataSeeder"
                    };
                    _db.LogbookEntries.Add(entry);
                    entryCount++;

                    currentDate = currentDate.AddDays(1);
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} logbook entries.", entryCount);
        }

        private async Task SeedLogbookTasksAsync()
        {
            _logger.LogInformation("Seeding logbook tasks...");

            var taskCount = 0;
            var confirmedPlacementStudents = _placementStudents
                .Where(ps => ps.Status == "confirmed")
                .ToList();

            foreach (var ps in confirmedPlacementStudents)
            {
                var entries = await _db.LogbookEntries
                    .Where(e => e.PlacementStudentId == ps.Id)
                    .ToListAsync();

                foreach (var entry in entries)
                {
                    // 1-2 tasks per entry
                    var numTasks = TestDataGenerator.GetRandomInt(1, 2);
                    for (int i = 0; i < numTasks; i++)
                    {
                        var task = new LogbookTask
                        {
                            Id = Guid.NewGuid(),
                            TenantId = ps.TenantId,
                            PlacementStudentId = ps.Id,
                            Description = TestDataGenerator.GetLogbookTaskDescription(),
                            DatePerformed = entry.Date,
                            CreatedBy = "TestDataSeeder"
                        };
                        _db.LogbookTasks.Add(task);
                        taskCount++;
                    }
                }
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} logbook tasks.", taskCount);
        }

        private async Task SeedLogbookEvaluationsAsync()
        {
            _logger.LogInformation("Seeding logbook evaluations...");

            var evalCount = 0;
            var confirmedPlacementStudents = _placementStudents
                .Where(ps => ps.Status == "confirmed")
                .ToList();

            foreach (var ps in confirmedPlacementStudents)
            {
                // Calculate total hours
                var totalHours = await _db.LogbookEntries
                    .Where(e => e.PlacementStudentId == ps.Id)
                    .SumAsync(e => e.TotalHoursWorked);

                // Only create evaluation if 55+ hours
                if (totalHours < 55) continue;

                var placement = _placements.First(p => p.Id == ps.PlacementId);
                var supervisor = _supervisors.FirstOrDefault(s => s.Id == placement.SupervisorId);

                var evaluation = new LogbookEvaluation
                {
                    Id = Guid.NewGuid(),
                    TenantId = ps.TenantId,
                    PlacementStudentId = ps.Id,
                    AttendancePunctuality = GetRandomRating(),
                    Appearance = GetRandomRating(),
                    CommunicationSkills = GetRandomRating(),
                    Initiative = GetRandomRating(),
                    WorkQuality = GetRandomRating(),
                    Teamwork = GetRandomRating(),
                    SafetyAwareness = GetRandomRating(),
                    OverallPerformance = GetRandomRating(),
                    SupervisorName = supervisor?.FullName ?? "Test Supervisor",
                    Comments = "Student performed well during the placement period and showed good attitude.",
                    SupervisorSignedAt = DateTimeOffset.UtcNow.AddDays(-TestDataGenerator.GetRandomInt(0, 3)),
                    CreatedBy = "TestDataSeeder"
                };
                _db.LogbookEvaluations.Add(evaluation);
                evalCount++;
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Seeded {Count} logbook evaluations.", evalCount);
        }

        private static PerformanceRating GetRandomRating()
        {
            // Bias towards positive ratings
            var value = TestDataGenerator.GetRandomInt(1, 10);
            return value switch
            {
                <= 1 => PerformanceRating.Unsatisfactory,
                <= 3 => PerformanceRating.Satisfactory,
                <= 7 => PerformanceRating.High,
                _ => PerformanceRating.VeryHigh
            };
        }
    }
}

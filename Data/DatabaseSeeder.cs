using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Apiary.Models;
using Apiary.Models.School;

namespace Apiary.Data
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var provider = scope.ServiceProvider;
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

            try
            {
                var db = provider.GetRequiredService<ApplicationDbContext>();
                var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = provider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

                // Repair migration history if needed (database exists but history was cleared)
                await RepairMigrationHistoryIfNeededAsync(db, logger);

                // Apply pending migrations (safe in development; remove if you don't want automatic migrations)
                await db.Database.MigrateAsync();

                // Seed roles BEFORE creating users
                await SeedRolesAsync(roleManager, logger);

                // Insert a School named "Admin" if it doesn't exist
                School? adminSchool = await db.Schools.FirstOrDefaultAsync(s => s.Name == "Admin");
                if (adminSchool == null)
                {
                    adminSchool = new School
                    {
                        Name = "Admin",
                        TenantKey = "Admin",
                        Timezone = "UTC"
                    };
                    db.Schools.Add(adminSchool);
                    await db.SaveChangesAsync();
                    logger.LogInformation("Seeded School 'Admin'");
                }

                // Create admin user if no users exist
                var hasUsers = await userManager.Users.AnyAsync();
                if (!hasUsers)
                {
                    var adminUser = new ApplicationUser
                    {
                        UserName = "adminsean",
                        DisplayName = "Admin",
                        Email = "admin@futureready.local",
                        IsActive = true,
                        TenantId = adminSchool.Id
                    };

                    var result = await userManager.CreateAsync(adminUser, "Undivided-Reputable-Bartender8");

                    if (result.Succeeded)
                    {
                        // Assign Site Admin role to the default admin user
                        await userManager.AddToRoleAsync(adminUser, Roles.SiteAdmin);
                        logger.LogInformation("Seeded admin user 'adminsean' with Site Admin role");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            logger.LogError("Error creating admin user: {Error}", error.Description);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private static async Task RepairMigrationHistoryIfNeededAsync(ApplicationDbContext db, ILogger logger)
        {
            // Check if the Schools table exists (indicating DB was created) but migration history is empty
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Schools')
                       AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory])
                    SELECT 1 ELSE SELECT 0";

                var needsRepair = (int)(await command.ExecuteScalarAsync() ?? 0) == 1;

                if (needsRepair)
                {
                    logger.LogWarning("Migration history is empty but database exists. Repairing...");

                    using var insertCommand = connection.CreateCommand();
                    insertCommand.CommandText = @"
                        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES
                        ('20260120060626_InitialCreate', '10.0.0'),
                        ('20260121001302_RemoveCohortEntity', '10.0.0'),
                        ('20260121001846_AddSchoolContactFields', '10.0.0'),
                        ('20260121004246_UpdateStudentFields', '10.0.0'),
                        ('20260121005039_AddEmergencyContacts', '10.0.0'),
                        ('20260121040848_AddStudentMedicalConditions', '10.0.0'),
                        ('20260121041647_AddCompanyEntity', '10.0.0'),
                        ('20260121042236_AddSupervisorEntity', '10.0.0'),
                        ('20260121044507_AddPlacementAndParentPermission', '10.0.0'),
                        ('20260121090851_AddFormTokenEntity', '10.0.0'),
                        ('20260122130944_AddLogbookModels', '10.0.0'),
                        ('20260122132328_AddEvaluationAndWorkHistoryModels', '10.0.0'),
                        ('20260123042305_MigrateToIdentity', '10.0.0'),
                        ('20260208060711_AddStudentEmailAndAccountToken', '10.0.0'),
                        ('20260310090935_MultiStudentPlacements', '10.0.0'),
                        ('20260310094441_AddPlacementRole', '10.0.0'),
                        ('20260316035804_DropStudentWorkHistoriesTable', '10.0.0'),
                        ('20260319114253_AddPlacementDates', '10.0.0'),
                        ('20260320084505_DropLogbookEntryCumulativeHours', '10.0.0')";
                    await insertCommand.ExecuteNonQueryAsync();

                    logger.LogInformation("Migration history repaired.");
                }
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
        {
            foreach (var roleName in Roles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole<Guid>(roleName);
                    var result = await roleManager.CreateAsync(role);

                    if (result.Succeeded)
                    {
                        logger.LogInformation("Created role: {RoleName}", roleName);
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            logger.LogError("Error creating role {RoleName}: {Error}", roleName, error.Description);
                        }
                    }
                }
            }
        }
    }
}

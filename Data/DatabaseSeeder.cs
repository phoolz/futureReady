using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FutureReady.Models;
using FutureReady.Models.School;

namespace FutureReady.Data
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

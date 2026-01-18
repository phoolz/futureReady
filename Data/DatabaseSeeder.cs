using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FutureReady.Models;
using FutureReady.Models.School;
using FutureReady.Services;

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

                // Apply pending migrations (safe in development; remove if you don't want automatic migrations)
                await db.Database.MigrateAsync();

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
                var hasUsers = await db.Users.AnyAsync();
                if (!hasUsers)
                {
                    var adminUser = new User
                    {
                        UserName = "adminsean",
                        DisplayName = "Admin",
                        Email = "admin@futureready.local",
                        PasswordHash = PasswordHasher.HashPassword("Undivided-Reputable-Bartender8"),
                        IsActive = true,
                        TenantId = adminSchool.Id
                    };
                    db.Users.Add(adminUser);
                    await db.SaveChangesAsync();
                    logger.LogInformation("Seeded admin user 'adminsean'");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }
}


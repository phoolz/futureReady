using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

                // Apply pending migrations (safe in development; remove if you don't want automatic migrations)
                await db.Database.MigrateAsync();

                // Insert a School named "Admin" if it doesn't exist
                var exists = await db.Schools.AnyAsync(s => s.Name == "Admin");
                if (!exists)
                {
                    db.Schools.Add(new School
                    {
                        Name = "Admin",
                        TenantKey = "Admin",
                        Timezone = "UTC"
                    });

                    await db.SaveChangesAsync();
                    logger.LogInformation("Seeded School 'Admin'");
                }
                else
                {
                    logger.LogInformation("School 'Admin' already exists; skipping seeding.");
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


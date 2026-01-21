using Microsoft.EntityFrameworkCore;
using FutureReady.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FutureReady.Services;
using FutureReady.Models.School;
    
namespace FutureReady.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IUserProvider? _userProvider;
        private readonly ITenantProvider? _tenantProvider;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserProvider? userProvider = null, ITenantProvider? tenantProvider = null) : base(options)
        {
            _userProvider = userProvider;
            _tenantProvider = tenantProvider;
        }

        public DbSet<School> Schools { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Teacher> Teachers { get; set; } = null!;
        public DbSet<EmergencyContact> EmergencyContacts { get; set; } = null!;
        public DbSet<StudentMedicalCondition> StudentMedicalConditions { get; set; } = null!;
        public DbSet<Company> Companies { get; set; } = null!;
        public DbSet<Supervisor> Supervisors { get; set; } = null!;
        public DbSet<Placement> Placements { get; set; } = null!;
        public DbSet<ParentPermission> ParentPermissions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.UserName).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Apply configuration for BaseEntity-derived types
            modelBuilder.Model.GetEntityTypes()
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType))
                .ToList()
                .ForEach(entityType =>
                {
                    // Apply global query filter for soft delete
                    var method = typeof(ApplicationDbContext).GetMethod(nameof(SetSoftDeleteFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                        ?.MakeGenericMethod(entityType.ClrType);
                    method?.Invoke(null, new object[] { modelBuilder });

                    // Configure RowVersion as concurrency token
                    modelBuilder.Entity(entityType.ClrType).Property("RowVersion").IsRowVersion();
                });

            modelBuilder.Entity<School>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.TenantKey).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Timezone).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.TenantKey).IsUnique();
            });

            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.HasIndex(e => e.UserId).IsUnique(); // enforce one-to-one user->teacher
                entity.HasOne(e => e.User).WithOne().HasForeignKey<Teacher>(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.School).WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PreferredName).HasMaxLength(100);
                entity.Property(e => e.StudentNumber).HasMaxLength(50);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.StudentType).HasMaxLength(20);
                entity.Property(e => e.YearLevel).HasMaxLength(20);
                entity.Property(e => e.MedicareNumber).HasMaxLength(100);
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<EmergencyContact>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.MobileNumber).HasMaxLength(20);
                entity.Property(e => e.Relationship).HasMaxLength(50);
                entity.Property(e => e.IsPrimary).IsRequired();
                entity.HasOne(e => e.Student).WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StudentMedicalCondition>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ConditionType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Details).HasMaxLength(2000);
                entity.HasOne(e => e.Student).WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Industry).HasMaxLength(100);
                entity.Property(e => e.StreetAddress).HasMaxLength(200);
                entity.Property(e => e.StreetAddress2).HasMaxLength(200);
                entity.Property(e => e.Suburb).HasMaxLength(100);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.State).HasMaxLength(50);
                entity.Property(e => e.PostalCode).HasMaxLength(20);
                entity.Property(e => e.PostalStreetAddress).HasMaxLength(200);
                entity.Property(e => e.PostalSuburb).HasMaxLength(100);
                entity.Property(e => e.PostalCity).HasMaxLength(100);
                entity.Property(e => e.PostalState).HasMaxLength(50);
                entity.Property(e => e.PostalPostalCode).HasMaxLength(20);
                entity.Property(e => e.InsuranceValue).HasMaxLength(50);
            });

            modelBuilder.Entity<Supervisor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.JobTitle).HasMaxLength(100);
                entity.Property(e => e.Email).HasMaxLength(200);
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Placement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.DressRequirement).HasMaxLength(500);
                entity.Property(e => e.WorkStartTime).HasMaxLength(10);
                entity.Property(e => e.WorkEndTime).HasMaxLength(10);
                entity.Property(e => e.SafetyBriefingMethod).HasMaxLength(500);
                entity.Property(e => e.EmployerDriverExperience).HasMaxLength(500);
                entity.Property(e => e.EmployerLicenceType).HasMaxLength(100);
                entity.HasOne(e => e.Student).WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Company).WithMany().HasForeignKey(e => e.CompanyId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(e => e.Supervisor).WithMany().HasForeignKey(e => e.SupervisorId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ParentPermission>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TransportMethod).HasMaxLength(50);
                entity.Property(e => e.DriverName).HasMaxLength(200);
                entity.Property(e => e.DriverContactNumber).HasMaxLength(50);
                entity.Property(e => e.ParentFirstName).HasMaxLength(100);
                entity.Property(e => e.ParentLastName).HasMaxLength(100);
                entity.HasOne(e => e.Placement).WithMany().HasForeignKey(e => e.PlacementId).OnDelete(DeleteBehavior.Cascade);
            });
        }

        private static void SetSoftDeleteFilter<TEntity>(ModelBuilder builder) where TEntity : BaseEntity
        {
            builder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
        }

        public override int SaveChanges()
        {
            ApplyAuditRules();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditRules();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyAuditRules()
        {
            var now = DateTimeOffset.UtcNow;
            var user = _userProvider?.GetCurrentUsername() ?? "system";
            var tenantId = _tenantProvider?.GetCurrentTenantId();

            foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is BaseEntity))
            {
                var entity = (BaseEntity)entry.Entity;
                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.CreatedAt = now;
                        entity.CreatedBy = entity.CreatedBy ?? user;

                        // If the entity is tenant-aware, and we have a tenant, set it
                        if (entity is TenantEntity te && te.TenantId == Guid.Empty && tenantId.HasValue)
                        {
                            te.TenantId = tenantId.Value;
                        }

                        break;
                    case EntityState.Modified:
                        entity.UpdatedAt = now;
                        entity.UpdatedBy = user;
                        break;
                    case EntityState.Deleted:
                        // Convert hard delete into soft delete
                        entity.IsDeleted = true;
                        entity.DeletedAt = now;
                        entity.DeletedBy = user;
                        entry.State = EntityState.Modified;
                        break;
                }
            }
        }
    }
}

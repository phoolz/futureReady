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
        public DbSet<Cohort> Cohorts { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<Teacher> Teachers { get; set; } = null!;
        public DbSet<CohortTeacher> CohortTeachers { get; set; } = null!;

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

            modelBuilder.Entity<Cohort>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.GraduationYear).IsRequired();
                entity.Property(e => e.GraduationMonth).IsRequired();
                entity.HasOne(e => e.School).WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.Cascade);
                // Keep table name matching the existing migration (was SchoolCohorts)
                entity.ToTable("SchoolCohorts");
            });

            modelBuilder.Entity<Teacher>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();
                entity.HasIndex(e => e.UserId).IsUnique(); // enforce one-to-one user->teacher
                entity.HasOne(e => e.User).WithOne().HasForeignKey<Teacher>(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.School).WithMany().HasForeignKey(e => e.SchoolId).OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CohortTeacher>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TeacherId).IsRequired();
                entity.Property(e => e.CohortId).IsRequired();
                entity.Property(e => e.IsSubstitute).IsRequired();
                entity.HasOne(e => e.Teacher).WithMany().HasForeignKey(e => e.TeacherId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Cohort).WithMany().HasForeignKey(e => e.CohortId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MedicareNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.StudentType).IsRequired().HasMaxLength(100);
                entity.HasOne(e => e.Cohort).WithMany().HasForeignKey(e => e.CohortId).OnDelete(DeleteBehavior.Cascade);
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

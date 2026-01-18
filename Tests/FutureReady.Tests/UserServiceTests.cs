using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FutureReady.Data;
using FutureReady.Models;
using FutureReady.Services;
using FutureReady.Services.Users;

namespace FutureReady.Tests
{
    public class UserServiceTests
    {
        private class FakeUserProvider : IUserProvider
        {
            public string? GetCurrentUsername() => "test-user";
        }

        private class FakeTenantProvider : ITenantProvider
        {
            private readonly Guid _id;
            public FakeTenantProvider(Guid? tenantId = null) => _id = tenantId ?? Guid.NewGuid();
            public Guid? GetCurrentTenantId() => _id;
        }

        private (ApplicationDbContext Context, Microsoft.Data.Sqlite.SqliteConnection Connection, UserService Service, Guid TenantId) CreateTestContext()
        {
            var tenantId = Guid.NewGuid();
            var tenantProvider = new FakeTenantProvider(tenantId);
            var userProvider = new FakeUserProvider();

            var (context, connection) = TestDbContextFactory.CreateSqliteInMemoryContext(userProvider, tenantProvider);
            var service = new UserService(context);

            return (context, connection, service, tenantId);
        }

        private User CreateTestUser(Guid tenantId, string userName = "testuser", string email = "test@example.com", string? password = null)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserName = userName,
                DisplayName = "Test User",
                Email = email,
                PasswordHash = password,
                IsActive = true
            };
        }

        #region Create Operations

        [Fact]
        public async Task CreateAsync_ValidUser_CreatesSuccessfully()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = CreateTestUser(tenantId);

                await service.CreateAsync(user);

                var saved = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
                Assert.NotNull(saved);
                Assert.Equal("testuser", saved!.UserName);
                Assert.Equal("test@example.com", saved.Email);
                Assert.Equal("Test User", saved.DisplayName);
                Assert.True(saved.IsActive);
            }
        }

        [Fact]
        public async Task CreateAsync_HashesPassword()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = CreateTestUser(tenantId, password: "plaintext123");

                await service.CreateAsync(user);

                var saved = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
                Assert.NotNull(saved);
                Assert.NotNull(saved!.PasswordHash);
                Assert.NotEqual("plaintext123", saved.PasswordHash);
                Assert.True(PasswordHasher.VerifyPassword("plaintext123", saved.PasswordHash));
            }
        }

        [Fact]
        public async Task CreateAsync_NullPassword_CreatesWithoutHash()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = CreateTestUser(tenantId, password: null);

                await service.CreateAsync(user);

                var saved = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
                Assert.NotNull(saved);
                Assert.Null(saved!.PasswordHash);
            }
        }

        [Fact]
        public async Task CreateAsync_EmptyPassword_CreatesWithoutHash()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = CreateTestUser(tenantId, password: "   ");

                await service.CreateAsync(user);

                var saved = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
                Assert.NotNull(saved);
                Assert.Null(saved!.PasswordHash);
            }
        }

        #endregion

        #region Read Operations

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user1 = CreateTestUser(tenantId, "user1", "user1@example.com");
                var user2 = CreateTestUser(tenantId, "user2", "user2@example.com");

                await service.CreateAsync(user1);
                await service.CreateAsync(user2);

                var users = await service.GetAllAsync();

                Assert.Equal(2, users.Count);
            }
        }

        [Fact]
        public async Task GetAllAsync_ExcludesSoftDeletedUsers()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user1 = CreateTestUser(tenantId, "user1", "user1@example.com");
                var user2 = CreateTestUser(tenantId, "user2", "user2@example.com");

                await service.CreateAsync(user1);
                await service.CreateAsync(user2);

                // Soft delete user1
                await service.DeleteAsync(user1.Id);

                var users = await service.GetAllAsync();

                Assert.Single(users);
                Assert.Equal("user2", users[0].UserName);
            }
        }

        [Fact]
        public async Task GetByIdAsync_ExistingUser_ReturnsUser()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = CreateTestUser(tenantId);

                await service.CreateAsync(user);

                var found = await service.GetByIdAsync(user.Id);

                Assert.NotNull(found);
                Assert.Equal(user.Id, found!.Id);
                Assert.Equal("testuser", found.UserName);
            }
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingUser_ReturnsNull()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var found = await service.GetByIdAsync(Guid.NewGuid());

                Assert.Null(found);
            }
        }

        [Fact]
        public async Task GetByIdAsync_SoftDeletedUser_ReturnsNull()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = CreateTestUser(tenantId);
                await service.CreateAsync(user);

                await service.DeleteAsync(user.Id);

                var found = await service.GetByIdAsync(user.Id);

                Assert.Null(found);
            }
        }

        #endregion

        #region Update Operations

        [Fact]
        public async Task UpdateAsync_ValidUser_UpdatesSuccessfully()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = CreateTestUser(tenantId);
                await service.CreateAsync(user);

                var updated = new User
                {
                    Id = user.Id,
                    TenantId = tenantId,
                    UserName = "updateduser",
                    DisplayName = "Updated User",
                    Email = "updated@example.com",
                    IsActive = false
                };

                await service.UpdateAsync(updated);

                var saved = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
                Assert.NotNull(saved);
                Assert.Equal("updateduser", saved!.UserName);
                Assert.Equal("Updated User", saved.DisplayName);
                Assert.Equal("updated@example.com", saved.Email);
                Assert.False(saved.IsActive);
            }
        }

        [Fact]
        public async Task UpdateAsync_WithPassword_HashesNewPassword()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = CreateTestUser(tenantId, password: "oldpassword");
                await service.CreateAsync(user);

                var oldHash = (await context.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id)).PasswordHash;

                var updated = new User
                {
                    Id = user.Id,
                    TenantId = tenantId,
                    UserName = user.UserName,
                    Email = user.Email,
                    PasswordHash = "newpassword123",
                    IsActive = true
                };

                await service.UpdateAsync(updated);

                var saved = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
                Assert.NotNull(saved);
                Assert.NotEqual(oldHash, saved!.PasswordHash);
                Assert.True(PasswordHasher.VerifyPassword("newpassword123", saved.PasswordHash!));
            }
        }

        [Fact]
        public async Task UpdateAsync_NonExistingUser_ThrowsException()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserName = "nonexistent",
                    Email = "nonexistent@example.com"
                };

                await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateAsync(user));
            }
        }

        [Fact]
        public async Task UpdateAsync_WithStaleRowVersion_ThrowsConcurrencyException()
        {
            // This test verifies that passing a stale row version triggers a concurrency exception.
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = CreateTestUser(tenantId);
                await service.CreateAsync(user);

                // Use a fake/stale row version that won't match
                var staleRowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

                var updated = new User
                {
                    Id = user.Id,
                    TenantId = tenantId,
                    UserName = "updateduser",
                    Email = user.Email,
                    IsActive = true
                };

                // This should throw a concurrency exception because the row version doesn't match
                await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
                    service.UpdateAsync(updated, staleRowVersion));
            }
        }

        #endregion

        #region Delete Operations

        [Fact]
        public async Task DeleteAsync_ExistingUser_SoftDeletes()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                var user = CreateTestUser(tenantId);
                await service.CreateAsync(user);

                await service.DeleteAsync(user.Id);

                // User should not be found via normal query (due to soft delete filter)
                var notFound = await service.GetByIdAsync(user.Id);
                Assert.Null(notFound);

                // But should exist when ignoring query filters
                var deleted = await context.Users.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
                Assert.NotNull(deleted);
                Assert.True(deleted!.IsDeleted);
                Assert.NotNull(deleted.DeletedAt);
                Assert.Equal("test-user", deleted.DeletedBy);
            }
        }

        [Fact]
        public async Task DeleteAsync_NonExistingUser_NoError()
        {
            var (context, connection, service, tenantId) = CreateTestContext();
            using (context)
            using (connection)
            {
                // Should not throw
                await service.DeleteAsync(Guid.NewGuid());
            }
        }

        #endregion
    }
}

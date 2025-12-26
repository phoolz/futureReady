// csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FutureReady.Data;
using FutureReady.Models;
using FutureReady.Models.School;
using FutureReady.Services;

namespace FutureReady.Tests
{
    public class AuditTests
    {
        private class FakeUserProvider : IUserProvider
        {
            public string? GetCurrentUsername() => "test-user";
        }

        private class FakeTenantProvider : ITenantProvider
        {
            private readonly Guid _id = Guid.NewGuid();
            public Guid? GetCurrentTenantId() => _id;
        }

        [Fact]
        public async Task AddEntity_SetsAuditAndTenant()
        {
            var tenantProvider = new FakeTenantProvider();
            var userProvider = new FakeUserProvider();

            var tuple = TestDbContextFactory.CreateSqliteInMemoryContext(userProvider, tenantProvider);
            using var ctx = tuple.Context;
            using var connection = tuple.Connection;

            var school = new School { Name = "Test School", TenantKey = "tkey", Timezone = "UTC" };
            ctx.Schools.Add(school);
            await ctx.SaveChangesAsync();

            var saved = ctx.Schools.AsNoTracking().FirstOrDefault();
            Assert.NotNull(saved);
            Assert.NotEqual(default, saved!.CreatedAt);
            Assert.Equal("test-user", saved.CreatedBy);
        }

        [Fact]
        public async Task DeleteEntity_SetsSoftDelete()
        {
            var tenantProvider = new FakeTenantProvider();
            var userProvider = new FakeUserProvider();

            var tuple = TestDbContextFactory.CreateSqliteInMemoryContext(userProvider, tenantProvider);
            using var ctx = tuple.Context;
            using var connection = tuple.Connection;

            var school = new School { Name = "DeleteMe", TenantKey = "tkey2", Timezone = "UTC" };
            ctx.Schools.Add(school);
            await ctx.SaveChangesAsync();

            ctx.Schools.Remove(school);
            await ctx.SaveChangesAsync();

            var all = ctx.Schools.IgnoreQueryFilters().AsNoTracking().FirstOrDefault();
            Assert.NotNull(all);
            Assert.True(all!.IsDeleted);
            Assert.NotNull(all.DeletedAt);
            Assert.Equal("test-user", all.DeletedBy);
        }
    }
}
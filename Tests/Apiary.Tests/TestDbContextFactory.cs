using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Apiary.Data;
using Apiary.Services;
using Microsoft.Data.Sqlite;

namespace Apiary.Tests
{
    /// <summary>
    /// Test-specific DbContext that adds SQLite DateTimeOffset support.
    /// SQLite does not natively support DateTimeOffset in ORDER BY clauses,
    /// so we convert DateTimeOffset to ticks for sorting compatibility.
    /// </summary>
    public class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserProvider? userProvider = null, ITenantProvider? tenantProvider = null)
            : base(options, userProvider, tenantProvider)
        {
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            // Configure DateTimeOffset to store as ticks for SQLite compatibility in ORDER BY clauses
            configurationBuilder.Properties<DateTimeOffset>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
            configurationBuilder.Properties<DateTimeOffset?>()
                .HaveConversion<DateTimeOffsetToBinaryConverter>();
        }
    }

    public static class TestDbContextFactory
    {
        // Creates an ApplicationDbContext backed by a single open SQLite in-memory connection.
        // Returns the context and the open connection; caller should Dispose/Close the connection after use.
        public static (ApplicationDbContext Context, SqliteConnection Connection) CreateSqliteInMemoryContext(IUserProvider? user = null, ITenantProvider? tenant = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new TestApplicationDbContext(options, user, tenant);
            context.Database.EnsureCreated();

            return (context, connection);
        }

        // Create a context using an existing open SQLite connection (useful to share the same DB across multiple contexts)
        public static ApplicationDbContext CreateSqliteContextFromOpenConnection(SqliteConnection connection, IUserProvider? user = null, ITenantProvider? tenant = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new TestApplicationDbContext(options, user, tenant);
            context.Database.EnsureCreated();

            return context;
        }
    }
}

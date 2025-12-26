using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Services;
using Microsoft.Data.Sqlite;

namespace FutureReady.Tests
{
    public static class TestDbContextFactory
    {
        // Creates an ApplicationDbContext backed by a single open SQLite in-memory connection.
        // Returns the context and the open connection; caller should Dispose/Close the connection after use.
        public static (ApplicationDbContext Context, SqliteConnection Connection) CreateSqliteInMemoryContext(IUserProvider? user = null, ITenantProvider? tenant = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connection)
                .Options;

            var context = new ApplicationDbContext(options, user, tenant);
            context.Database.EnsureCreated();

            return (context, connection);
        }

        // Create a context using an existing open SQLite connection (useful to share the same DB across multiple contexts)
        public static ApplicationDbContext CreateSqliteContextFromOpenConnection(SqliteConnection connection, IUserProvider? user = null, ITenantProvider? tenant = null)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connection)
                .Options;

            var context = new ApplicationDbContext(options, user, tenant);
            context.Database.EnsureCreated();

            return context;
        }
    }
}

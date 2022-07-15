using System;
using Api.Database.Context;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Test
{
    // Class for building and disposing dbcontext
    public class DatabaseFixture : IDisposable
    {
        public FlotillaDbContext NewContext => CreateContext();
        private SqliteConnection? _connection;

        private DbContextOptions<FlotillaDbContext> CreateOptions()
        {
            //if (_connection is null)
            //{
            string connectionString = new SqliteConnectionStringBuilder { DataSource = ":memory:", Cache = SqliteCacheMode.Shared }.ToString();
            _connection = new SqliteConnection(connectionString);
            _connection.Open();
            //}
            var builder = new DbContextOptionsBuilder<FlotillaDbContext>();
            builder.EnableSensitiveDataLogging();
            builder.UseSqlite(_connection);
            return builder.Options;
        }

        public FlotillaDbContext CreateContext()
        {
            var options = CreateOptions();
            var context = new FlotillaDbContext(options);
            context.Database.EnsureCreated();
            InitDb.PopulateDb(context);
            return context;
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
            GC.SuppressFinalize(this);
        }

    }

    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}

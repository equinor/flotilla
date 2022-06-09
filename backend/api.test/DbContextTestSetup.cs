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
        public FlotillaDbContext Context { get; private set; }
        private readonly SqliteConnection _connection;

        public DatabaseFixture()
        {
            var builder = new DbContextOptionsBuilder<FlotillaDbContext>();
            string connectionString = new SqliteConnectionStringBuilder { DataSource = ":memory:", Cache = SqliteCacheMode.Shared }.ToString();
            _connection = new SqliteConnection(connectionString);
            _connection.Open();
            builder.EnableSensitiveDataLogging();
            builder.UseSqlite(_connection);
            Context = new FlotillaDbContext(builder.Options);
            Context.Database.EnsureCreated();
            InitDb.PopulateDb(Context);
        }

        public void Dispose()
        {
            _connection.Close();
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

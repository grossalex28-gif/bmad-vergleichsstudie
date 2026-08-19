using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;

namespace seed_a_backend.Tests;

/// <summary>
/// Provides a shared-cache in-memory SQLite database per test so BookingService can be
/// exercised through real transactions and the filtered unique index that enforces A-F13,
/// which the EF Core InMemory provider cannot express.
/// </summary>
public sealed class SqliteDbContextFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDbContextFixture()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new AppDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}

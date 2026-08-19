using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;

namespace seed_a_backend.Tests;

/// Baut eine SQLite-In-Memory-Datenbank auf, die (anders als der EF-Core-InMemory-Provider)
/// echte Unique-Constraints inklusive des gefilterten Index auf aktive Sitzplatzbelegungen
/// durchsetzt. So lässt sich das Verhalten bei gleichzeitigen Buchungsversuchen (A-F13) testen.
public sealed class SqliteDbContextFactory : IDisposable
{
    private readonly string _connectionString;

    // Hält eine Verbindung zur benannten In-Memory-Datenbank offen, damit sie nicht
    // verschwindet, sobald ein einzelner AppDbContext seine Verbindung schließt.
    private readonly SqliteConnection _ankerVerbindung;

    public SqliteDbContextFactory()
    {
        _connectionString = $"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
        _ankerVerbindung = new SqliteConnection(_connectionString);
        _ankerVerbindung.Open();

        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        return new AppDbContext(options);
    }

    public void Dispose() => _ankerVerbindung.Dispose();
}

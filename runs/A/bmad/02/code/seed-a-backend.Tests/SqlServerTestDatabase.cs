using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Infrastructure;

namespace seed_a_backend.Tests;

/// <summary>
/// Erstellt für jede Testklasse eine eigene, eindeutig benannte Testdatenbank auf dem echten
/// SQL Server aus ConnectionStrings__Default (Consistency Convention „Test-Engine für
/// Nebenläufigkeit“ — kein EF-Core-InMemory-Provider, da gefilterte Unique-Constraints sonst
/// nicht durchgesetzt würden). Schema wird über die echten EF-Core-Migrations aufgebaut, damit
/// die Migration selbst mitgetestet wird.
/// </summary>
public sealed class SqlServerTestDatabase : IAsyncLifetime
{
    private readonly string _connectionString;

    public SqlServerTestDatabase()
    {
        _connectionString = CreateIsolatedConnectionString();
    }

    /// <summary>
    /// Baut eine eigene, per GUID benannte Connection String für eine isolierte Testdatenbank —
    /// wiederverwendbar für Tests, die (anders als diese Klasse) die Datenbank nicht vorab per
    /// Migration anlegen dürfen, weil genau das Anlegen selbst Teil des zu testenden Verhaltens ist.
    /// </summary>
    public static string CreateIsolatedConnectionString()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? throw new InvalidOperationException("ConnectionStrings__Default ist für die Tests nicht gesetzt.");

        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = $"seed_a_test_{Guid.NewGuid():N}",
        };
        return builder.ConnectionString;
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async ValueTask InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;

namespace seed_a_backend.Tests.TestSupport;

/// <summary>
/// Testinfrastruktur-Entscheidung (siehe Story Dev Notes "Testinfrastruktur"): Tests laufen gegen eine
/// echte, über <c>ConnectionStrings__Default</c> erreichbare SQL-Server-Instanz, da der EF-Core-InMemory-Provider
/// das JSON-Spalten-Mapping von <c>Raum.Reihen</c>/<c>Raum.GangSpalten</c> (AD-1) nicht zuverlässig abbildet.
/// Jede Testklasse erhält eine eigene, per Discriminator benannte Datenbank, die vor jedem Test frisch migriert wird.
/// </summary>
internal static class SqlServerTestDatabase
{
    public static string BuildConnectionString(string discriminator)
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings__Default ist nicht gesetzt. Die Backend-Tests benötigen eine erreichbare SQL-Server-Instanz.");

        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = new SqlConnectionStringBuilder(baseConnectionString).InitialCatalog + "_" + discriminator
        };
        return builder.ConnectionString;
    }

    public static async Task<AppDbContext> CreateFreshContextAsync(string discriminator)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildConnectionString(discriminator))
            .Options;

        var db = new AppDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        return db;
    }

    public static async Task DropAsync(string discriminator)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(BuildConnectionString(discriminator))
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureDeletedAsync();
    }
}

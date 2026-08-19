using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using seed_a_backend.Api.Data;

namespace seed_a_backend.Tests;

/// Startet die echte ASP.NET-Core-Pipeline aus Program.cs (inklusive Migrate + Seed-Loader),
/// ersetzt dabei aber den SQL-Server-Provider durch eine SQLite-In-Memory-Datenbank, damit die
/// Integrationstests ohne echten Datenbankserver laufen.
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _ankerVerbindung =
        new($"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared");

    public CustomWebApplicationFactory() => _ankerVerbindung.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Program.cs registriert AppDbContext bereits mit dem SqlServer-Provider. AddDbContext
            // hinterlegt die Konfigurationsdelegaten additiv (nicht ersetzend), daher reicht das
            // Entfernen von DbContextOptions<AppDbContext> allein nicht aus - sämtliche zu diesem
            // Kontext gehörenden Deskriptoren müssen vorher entfernt werden.
            var zuEntfernen = services
                .Where(d => (d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                             d.ServiceType == typeof(AppDbContext) ||
                             d.ServiceType.Name.Contains("DbContextOptionsConfiguration")))
                .ToList();
            foreach (var descriptor in zuEntfernen)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options
                .UseSqlite(_ankerVerbindung)
                // Die für SQL Server erzeugte Migrationshistorie weicht naturgemäß vom
                // SQLite-Modell ab (z. B. Spaltentypen); das ist für den Testzweck unerheblich.
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _ankerVerbindung.Dispose();
        }
    }
}

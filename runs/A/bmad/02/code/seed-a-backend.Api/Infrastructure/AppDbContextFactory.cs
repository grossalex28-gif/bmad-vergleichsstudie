using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace seed_a_backend.Api.Infrastructure;

/// <summary>
/// Wird ausschließlich von `dotnet ef` zur Design-Zeit verwendet (z. B. `migrations add`).
/// Der Platzhalter-Connection-String muss syntaktisch gültig sein, es wird zur Design-Zeit
/// keine tatsächliche Verbindung aufgebaut. Zur Laufzeit registriert Program.cs den echten
/// DbContext über ConnectionStrings__Default.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=DesignTimePlaceholder;Trusted_Connection=True;TrustServerCertificate=True;");
        return new AppDbContext(optionsBuilder.Options);
    }
}

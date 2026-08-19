using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;

namespace seed_a_backend.Tests;

public class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        Environment.GetEnvironmentVariable("ConnectionStrings__Default")
        ?? throw new InvalidOperationException("Umgebungsvariable ConnectionStrings__Default ist nicht gesetzt.");

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async ValueTask InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

[CollectionDefinition("Datenbank")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>;

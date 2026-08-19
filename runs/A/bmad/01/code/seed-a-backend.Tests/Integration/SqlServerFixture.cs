using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;

namespace seed_a_backend.Tests.Integration;

public class SqlServerFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = "";
    public bool IsAvailable { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            IsAvailable = false;
            return;
        }

        var connectionStringBuilder = new SqlConnectionStringBuilder(baseConnectionString);
        connectionStringBuilder.InitialCatalog += "_IntegrationTests";
        ConnectionString = connectionStringBuilder.ConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options;
        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.MigrateAsync();
        IsAvailable = true;
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options;
        return new AppDbContext(options);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

[CollectionDefinition("SqlServer")]
public class SqlServerCollection : ICollectionFixture<SqlServerFixture>;

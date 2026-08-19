using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

const string CorsPolicyName = "Frontend";
var frontendOrigins = builder.Configuration.GetSection("Frontend:Origins").Get<string[]>()
    ?? ["http://localhost:4200"];

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<BuchungService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(frontendOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // In der "Testing"-Umgebung (WebApplicationFactory mit SQLite) lässt sich die für
    // SQL Server erzeugte Migrationshistorie nicht anwenden; dort reicht EnsureCreated.
    if (app.Environment.IsEnvironment("Testing"))
    {
        await db.Database.EnsureCreatedAsync();
    }
    else
    {
        await db.Database.MigrateAsync();
    }

    var datenPfad = Path.Combine(app.Environment.ContentRootPath, "Data", "anfangsdatenbestand.json");
    await SeedDataLoader.LoadIfEmptyAsync(db, datenPfad);
}

app.UseCors(CorsPolicyName);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;

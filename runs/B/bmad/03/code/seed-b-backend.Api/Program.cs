using Microsoft.EntityFrameworkCore;
using seed_b_backend.Api.Data;
using seed_b_backend.Api.Data.Seeding;
using seed_b_backend.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<ProductQueryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<RatingService>();
builder.Services.AddScoped<CategoryQueryService>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedFilePath = Path.Combine(app.Environment.ContentRootPath, "Data", "anfangsdatenbestand.json");

    await DatabaseInitializer.MigrateAsync(db);
    await DatabaseInitializer.SeedIfEmptyAsync(db, seedFilePath);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Data;
using seed_a_backend.Api.Services;

var builder = WebApplication.CreateBuilder(args);

const string AngularDevServerPolicy = "AngularDevServer";

// Add services to the container.

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IVeranstaltungenService, VeranstaltungenService>();
builder.Services.AddScoped<IBuchungenService, BuchungenService>();
builder.Services.AddSingleton<IReferenzGenerator, ReferenzGenerator>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevServerPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var anfangsdatenbestandPfad = Path.Combine(app.Environment.ContentRootPath, "Data", "anfangsdatenbestand.json");
    await SeedLoader.SeedAsync(db, anfangsdatenbestandPfad);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(AngularDevServerPolicy);
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;

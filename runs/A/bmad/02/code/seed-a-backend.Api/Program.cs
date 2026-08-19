using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Application;
using seed_a_backend.Api.Controllers.Dtos;
using seed_a_backend.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(new ErrorEnvelopeDto(
                "VALIDATION_ERROR", "Die Anfrage enthält ungültige oder unvollständige Daten."));
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<SeedDataImporter>();
builder.Services.AddScoped<EventQueryService>();
builder.Services.AddScoped<EventDetailService>();
builder.Services.AddScoped<VenueQueryService>();
builder.Services.AddScoped<SeatMapService>();
builder.Services.AddScoped<BookingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorEnvelopeDto(
            "INTERNAL_ERROR", "Es ist ein unerwarteter Fehler aufgetreten."));
    });
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var importer = scope.ServiceProvider.GetRequiredService<SeedDataImporter>();
    var seedFilePath = app.Configuration["Seed:FilePath"]
        ?? Path.Combine(app.Environment.ContentRootPath, "Data", "anfangsdatenbestand.json");
    await importer.ImportIfEmptyAsync(seedFilePath);
}

app.Run();

/// <summary>Ermöglicht Integrationstests über `WebApplicationFactory&lt;Program&gt;`.</summary>
public partial class Program;

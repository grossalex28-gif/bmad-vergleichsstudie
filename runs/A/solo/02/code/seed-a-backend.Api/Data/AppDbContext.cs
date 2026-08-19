using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Models;

namespace seed_a_backend.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<PriceCategory> PriceCategories => Set<PriceCategory>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).HasMaxLength(50);
            entity.Property(v => v.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasMaxLength(50);
            entity.Property(r => r.Name).HasMaxLength(200).IsRequired();
            entity.HasOne(r => r.Venue)
                .WithMany(v => v.Rooms)
                .HasForeignKey(r => r.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000).IsRequired();
            entity.HasOne(e => e.Venue)
                .WithMany()
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Room)
                .WithMany(r => r.Events)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.StartsAt);
        });

        modelBuilder.Entity<PriceCategory>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).HasMaxLength(50);
            entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
            entity.Property(p => p.Price).HasPrecision(10, 2);
            entity.HasOne(p => p.Event)
                .WithMany(e => e.PriceCategories)
                .HasForeignKey(p => p.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Reference).HasMaxLength(12).IsRequired();
            entity.HasIndex(b => b.Reference).IsUnique();
            entity.Property(b => b.CustomerName).HasMaxLength(200).IsRequired();
            entity.Property(b => b.CustomerEmail).HasMaxLength(320).IsRequired();
            entity.HasOne(b => b.Event)
                .WithMany()
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BookingSeat>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.RowLabel).HasMaxLength(10).IsRequired();
            entity.Property(s => s.Price).HasPrecision(10, 2);
            entity.HasOne(s => s.Booking)
                .WithMany(b => b.Seats)
                .HasForeignKey(s => s.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(s => s.Event)
                .WithMany(e => e.BookingSeats)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(s => s.PriceCategory)
                .WithMany()
                .HasForeignKey(s => s.PriceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enforces A-F13: a seat can only be part of one active booking at a time.
            entity.HasIndex(s => new { s.EventId, s.RowLabel, s.Column })
                .IsUnique()
                .HasFilter("[IsCancelled] = 0")
                .HasDatabaseName("IX_BookingSeats_ActiveSeat");
        });
    }
}

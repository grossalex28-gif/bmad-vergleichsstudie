using Microsoft.EntityFrameworkCore;
using seed_a_backend.Api.Domain;

namespace seed_a_backend.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<PriceCategory> PriceCategories => Set<PriceCategory>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasMany(v => v.Rooms)
                .WithOne(r => r.Venue)
                .HasForeignKey(r => r.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(v => v.Events)
                .WithOne(e => e.Venue)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.PrimitiveCollection(r => r.Reihen);
            entity.PrimitiveCollection(r => r.GangSpalten);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasOne(e => e.Room)
                .WithMany()
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasOne(s => s.Room)
                .WithMany()
                .HasForeignKey(s => s.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.RoomId, s.Row, s.Column }).IsUnique();
        });

        modelBuilder.Entity<PriceCategory>(entity =>
        {
            entity.Property(pc => pc.Preis).HasColumnType("decimal(10,2)"); // AD-8

            entity.HasOne(pc => pc.Event)
                .WithMany()
                .HasForeignKey(pc => pc.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasIndex(b => b.Reference).IsUnique();

            entity.HasOne(b => b.Event)
                .WithMany()
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BookingSeat>(entity =>
        {
            entity.HasOne(bs => bs.Booking)
                .WithMany(b => b.Seats)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(bs => bs.Seat)
                .WithMany()
                .HasForeignKey(bs => bs.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(bs => bs.PriceCategory)
                .WithMany()
                .HasForeignKey(bs => bs.PriceCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // AD-1: einzige Quelle der Wahrheit für "belegt" — filtered unique index, nur aktive Zeilen zählen
            entity.HasIndex(bs => new { bs.EventId, bs.SeatId })
                .IsUnique()
                .HasFilter("[IsActive] = 1");
        });
    }
}

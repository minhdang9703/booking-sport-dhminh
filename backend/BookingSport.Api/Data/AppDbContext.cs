using BookingSport.Api.Entities;
using BookingSport.Api.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureCourts(modelBuilder);
        ConfigurePriceRules(modelBuilder);
        ConfigureBookings(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.PhoneNumber).IsUnique();
            entity.HasIndex(user => user.Email);

            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(255).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.PhoneNumber).HasMaxLength(30).IsRequired();
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(user => user.CreatedAt).IsRequired();
        });
    }

    private static void ConfigureCourts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Court>(entity =>
        {
            entity.HasKey(court => court.Id);
            entity.HasIndex(court => court.Name)
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");

            entity.Property(court => court.Name).HasMaxLength(120).IsRequired();
            entity.Property(court => court.CourtType).HasMaxLength(80).IsRequired();
            entity.Property(court => court.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(court => court.CreatedAt).IsRequired();
        });
    }

    private static void ConfigurePriceRules(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PriceRule>(entity =>
        {
            entity.HasKey(rule => rule.Id);
            entity.HasIndex(rule => new { rule.DayOfWeek, rule.StartTime, rule.EndTime });

            entity.Property(rule => rule.Name).HasMaxLength(150).IsRequired();
            entity.Property(rule => rule.DayOfWeek).IsRequired();
            entity.Property(rule => rule.StartTime).IsRequired();
            entity.Property(rule => rule.EndTime).IsRequired();
            entity.Property(rule => rule.HourlyPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(rule => rule.IsEnabled).IsRequired();
            entity.Property(rule => rule.CreatedAt).IsRequired();
        });
    }

    private static void ConfigureBookings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(booking => booking.Id);
            entity.HasIndex(booking => new { booking.CourtId, booking.BookingDate });
            entity.HasIndex(booking => new { booking.CourtId, booking.BookingDate, booking.StartTime, booking.EndTime })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL AND \"Status\" IN ('Pending', 'Confirmed')");

            entity.Property(booking => booking.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(booking => booking.StartTime).IsRequired();
            entity.Property(booking => booking.EndTime).IsRequired();
            entity.Property(booking => booking.HourlyPriceSnapshot).HasPrecision(18, 2).IsRequired();
            entity.Property(booking => booking.TotalPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(booking => booking.Note).HasMaxLength(500);
            entity.Property(booking => booking.CreatedAt).IsRequired();
            entity.Property(booking => booking.PaymentType).IsRequired();

            entity.HasOne(booking => booking.User)
                .WithMany(user => user.Bookings)
                .HasForeignKey(booking => booking.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(booking => booking.Court)
                .WithMany()
                .HasForeignKey(booking => booking.CourtId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

}

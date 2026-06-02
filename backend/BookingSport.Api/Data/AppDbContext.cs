using BookingSport.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Sport> Sports => Set<Sport>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Court> Courts => Set<Court>();
    public DbSet<CourtSchedule> CourtSchedules => Set<CourtSchedule>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUsers(modelBuilder);
        ConfigureSports(modelBuilder);
        ConfigureVenues(modelBuilder);
        ConfigureCourts(modelBuilder);
        ConfigureCourtSchedules(modelBuilder);
        ConfigureBookings(modelBuilder);
        ConfigurePayments(modelBuilder);
    }

    private static void ConfigureUsers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();

            entity.Property(user => user.FullName).HasMaxLength(150).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(255).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.PhoneNumber).HasMaxLength(30);
            entity.Property(user => user.Role).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(user => user.CreatedAt).IsRequired();
        });
    }

    private static void ConfigureSports(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sport>(entity =>
        {
            entity.HasKey(sport => sport.Id);
            entity.HasIndex(sport => sport.Name).IsUnique();

            entity.Property(sport => sport.Name).HasMaxLength(100).IsRequired();
            entity.Property(sport => sport.Description).HasMaxLength(500);
            entity.Property(sport => sport.CreatedAt).IsRequired();
        });
    }

    private static void ConfigureVenues(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(venue => venue.Id);
            entity.HasIndex(venue => new { venue.OwnerId, venue.Name });

            entity.Property(venue => venue.Name).HasMaxLength(150).IsRequired();
            entity.Property(venue => venue.Address).HasMaxLength(300).IsRequired();
            entity.Property(venue => venue.City).HasMaxLength(100);
            entity.Property(venue => venue.Description).HasMaxLength(1000);
            entity.Property(venue => venue.CreatedAt).IsRequired();

            entity.HasOne(venue => venue.Owner)
                .WithMany(user => user.Venues)
                .HasForeignKey(venue => venue.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCourts(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Court>(entity =>
        {
            entity.HasKey(court => court.Id);
            entity.HasIndex(court => new { court.VenueId, court.Name }).IsUnique();

            entity.Property(court => court.Name).HasMaxLength(120).IsRequired();
            entity.Property(court => court.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(court => court.Description).HasMaxLength(500);
            entity.Property(court => court.CreatedAt).IsRequired();

            entity.HasOne(court => court.Venue)
                .WithMany(venue => venue.Courts)
                .HasForeignKey(court => court.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(court => court.Sport)
                .WithMany(sport => sport.Courts)
                .HasForeignKey(court => court.SportId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCourtSchedules(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CourtSchedule>(entity =>
        {
            entity.HasKey(schedule => schedule.Id);
            entity.HasIndex(schedule => new
            {
                schedule.CourtId,
                schedule.DayOfWeek,
                schedule.StartTime,
                schedule.EndTime
            }).IsUnique();

            entity.Property(schedule => schedule.Price).HasPrecision(18, 2).IsRequired();
            entity.Property(schedule => schedule.CreatedAt).IsRequired();

            entity.HasOne(schedule => schedule.Court)
                .WithMany(court => court.Schedules)
                .HasForeignKey(schedule => schedule.CourtId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureBookings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(booking => booking.Id);
            entity.HasIndex(booking => new { booking.CourtScheduleId, booking.BookingDate });
            entity.HasIndex(booking => new { booking.CourtScheduleId, booking.BookingDate })
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL AND \"Status\" IN ('Pending', 'Confirmed')");

            entity.Property(booking => booking.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(booking => booking.TotalPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(booking => booking.Note).HasMaxLength(500);
            entity.Property(booking => booking.CreatedAt).IsRequired();

            entity.HasOne(booking => booking.User)
                .WithMany(user => user.Bookings)
                .HasForeignKey(booking => booking.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(booking => booking.CourtSchedule)
                .WithMany(schedule => schedule.Bookings)
                .HasForeignKey(booking => booking.CourtScheduleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePayments(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(payment => payment.Id);
            entity.HasIndex(payment => payment.BookingId).IsUnique();

            entity.Property(payment => payment.Method).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
            entity.Property(payment => payment.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(payment => payment.TransactionCode).HasMaxLength(100);
            entity.Property(payment => payment.CreatedAt).IsRequired();

            entity.HasOne(payment => payment.Booking)
                .WithOne(booking => booking.Payment)
                .HasForeignKey<Payment>(payment => payment.BookingId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

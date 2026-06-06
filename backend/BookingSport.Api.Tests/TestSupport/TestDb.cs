using BookingSport.Api.Data;
using BookingSport.Api.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BookingSport.Api.Tests.TestSupport;

public sealed class TestDb : IAsyncDisposable
{
    private TestDb(SqliteConnection connection, AppDbContext context)
    {
        Connection = connection;
        Context = context;
    }

    public SqliteConnection Connection { get; }
    public AppDbContext Context { get; }

    public static async Task<TestDb> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestAppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return new TestDb(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await Connection.DisposeAsync();
    }

    private sealed class TestAppDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        private static readonly ValueConverter<DateTimeOffset, long> DateTimeOffsetConverter = new(
            value => value.UtcTicks,
            value => new DateTimeOffset(value, TimeSpan.Zero));

        private static readonly ValueConverter<DateTimeOffset?, long?> NullableDateTimeOffsetConverter = new(
            value => value.HasValue ? value.Value.UtcTicks : null,
            value => value.HasValue ? new DateTimeOffset(value.Value, TimeSpan.Zero) : null);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureDateTimeOffset<User>(modelBuilder);
            ConfigureDateTimeOffset<Court>(modelBuilder);
            ConfigureDateTimeOffset<PriceRule>(modelBuilder);
            ConfigureDateTimeOffset<Booking>(modelBuilder);
        }

        private static void ConfigureDateTimeOffset<TEntity>(ModelBuilder modelBuilder)
            where TEntity : class
        {
            foreach (var property in modelBuilder.Entity<TEntity>().Metadata.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    modelBuilder.Entity<TEntity>()
                        .Property<DateTimeOffset>(property.Name)
                        .HasConversion(DateTimeOffsetConverter);
                }

                if (property.ClrType == typeof(DateTimeOffset?))
                {
                    modelBuilder.Entity<TEntity>()
                        .Property<DateTimeOffset?>(property.Name)
                        .HasConversion(NullableDateTimeOffsetConverter);
                }
            }
        }
    }
}

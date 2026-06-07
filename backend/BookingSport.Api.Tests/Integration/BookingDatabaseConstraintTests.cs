using BookingSport.Api.Data;
using BookingSport.Api.Entities;
using BookingSport.Api.Enums;
using BookingSport.Api.Tests.Integration.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BookingSport.Api.Tests.Integration;

[Collection(ApiIntegrationCollection.Name)]
public class BookingDatabaseConstraintTests(IntegrationTestFactory factory)
{
    [SkippableFact]
    public async Task DatabaseConstraint_WhenExactDuplicatePendingBookingIsInserted_RejectsSave()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        var seedIds = await SeedCoreDataAsync();

        await SaveBookingAsync(seedIds, BookingStatus.Pending, new TimeOnly(8, 0), new TimeOnly(9, 0));

        var act = () => SaveBookingAsync(seedIds, BookingStatus.Pending, new TimeOnly(8, 0), new TimeOnly(9, 0));

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [SkippableFact]
    public async Task DatabaseConstraint_WhenOverlappingBlockingBookingIsInserted_RejectsSave()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        var seedIds = await SeedCoreDataAsync();

        await SaveBookingAsync(seedIds, BookingStatus.Confirmed, new TimeOnly(8, 0), new TimeOnly(9, 0));

        var act = () => SaveBookingAsync(seedIds, BookingStatus.Pending, new TimeOnly(8, 30), new TimeOnly(9, 30));

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException.Should().BeOfType<PostgresException>()
            .Which.SqlState.Should().Be(PostgresErrorCodes.ExclusionViolation);
    }

    [SkippableTheory]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    public async Task DatabaseConstraint_WhenExistingBookingIsNotBlocking_AllowsOverlap(BookingStatus existingStatus)
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        var seedIds = await SeedCoreDataAsync();

        await SaveBookingAsync(seedIds, existingStatus, new TimeOnly(8, 0), new TimeOnly(9, 0));

        var act = () => SaveBookingAsync(seedIds, BookingStatus.Pending, new TimeOnly(8, 30), new TimeOnly(9, 30));

        await act.Should().NotThrowAsync();
    }

    [SkippableFact]
    public async Task DatabaseConstraint_WhenExistingBookingIsSoftDeleted_AllowsOverlap()
    {
        factory.SkipIfDockerUnavailable();
        await factory.ResetDatabaseAsync();
        var seedIds = await SeedCoreDataAsync();

        await SaveBookingAsync(
            seedIds,
            BookingStatus.Pending,
            new TimeOnly(8, 0),
            new TimeOnly(9, 0),
            deletedAt: DateTimeOffset.UtcNow);

        var act = () => SaveBookingAsync(seedIds, BookingStatus.Pending, new TimeOnly(8, 30), new TimeOnly(9, 30));

        await act.Should().NotThrowAsync();
    }

    private async Task<SeedIds> SeedCoreDataAsync()
    {
        SeedIds seedIds = default!;
        await factory.SeedAsync(dbContext =>
        {
            seedIds = IntegrationSeedData.AddCoreData(dbContext);
            return Task.CompletedTask;
        });

        return seedIds;
    }

    private async Task SaveBookingAsync(
        SeedIds seedIds,
        BookingStatus status,
        TimeOnly startTime,
        TimeOnly endTime,
        DateTimeOffset? deletedAt = null)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            UserId = seedIds.CustomerId,
            CourtId = seedIds.CourtId,
            BookingDate = IntegrationSeedData.BookingDate,
            StartTime = startTime,
            EndTime = endTime,
            Status = status,
            HourlyPriceSnapshot = 120_000m,
            TotalPrice = 120_000m,
            PaymentType = PaymentType.Cash,
            CreatedAt = DateTimeOffset.UtcNow,
            DeletedAt = deletedAt
        });

        await dbContext.SaveChangesAsync();
    }
}

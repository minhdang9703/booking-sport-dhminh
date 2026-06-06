using BookingSport.Api.DTOs.Courts;
using BookingSport.Api.Enums;
using BookingSport.Api.Services.Courts;
using BookingSport.Api.Tests.TestSupport;
using FluentAssertions;

namespace BookingSport.Api.Tests.Services.Courts;

public class CourtServiceTests
{
    [Fact]
    public async Task CreateCourtAsync_WithValidRequest_CreatesCourt()
    {
        await using var db = await TestDb.CreateAsync();
        var service = new CourtService(db.Context);

        var result = await service.CreateCourtAsync(new CourtCreateRequest
        {
            Name = " Court A ",
            CourtType = " Badminton ",
            Status = CourtStatus.Active
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Name.Should().Be("Court A");
        result.Value.CourtType.Should().Be("Badminton");
        db.Context.Courts.Should().ContainSingle(court => court.Name == "Court A");
    }

    [Theory]
    [InlineData("", "Badminton", "Court name is required.")]
    [InlineData("Court A", "", "CourtType is required.")]
    public async Task CreateCourtAsync_WithInvalidInput_ReturnsBadRequest(
        string name,
        string courtType,
        string expectedError)
    {
        await using var db = await TestDb.CreateAsync();
        var service = new CourtService(db.Context);

        var result = await service.CreateCourtAsync(new CourtCreateRequest
        {
            Name = name,
            CourtType = courtType
        }, CancellationToken.None);

        result.Status.Should().Be(CourtResultStatus.BadRequest);
        result.Error.Should().Be(expectedError);
    }

    [Fact]
    public async Task CreateCourtAsync_WhenActiveNameExists_ReturnsConflict()
    {
        await using var db = await TestDb.CreateAsync();
        db.Context.Courts.Add(TestData.Court(name: "Court A"));
        await db.Context.SaveChangesAsync();
        var service = new CourtService(db.Context);

        var result = await service.CreateCourtAsync(new CourtCreateRequest
        {
            Name = "Court A",
            CourtType = "Badminton"
        }, CancellationToken.None);

        result.Status.Should().Be(CourtResultStatus.Conflict);
    }

    [Fact]
    public async Task GetCourtsAsync_FiltersByStatusAndExcludesDeleted()
    {
        await using var db = await TestDb.CreateAsync();
        db.Context.Courts.AddRange(
            TestData.Court(name: "Court B", status: CourtStatus.Active),
            TestData.Court(name: "Court A", status: CourtStatus.Inactive),
            TestData.Court(name: "Court C", status: CourtStatus.Active, deletedAt: DateTimeOffset.UtcNow));
        await db.Context.SaveChangesAsync();
        var service = new CourtService(db.Context);

        var result = await service.GetCourtsAsync(new CourtQueryParameters
        {
            Status = CourtStatus.Active
        }, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Court B");
    }

    [Fact]
    public async Task GetCourtByIdAsync_WhenCourtIsDeleted_ReturnsNull()
    {
        await using var db = await TestDb.CreateAsync();
        var court = TestData.Court(deletedAt: DateTimeOffset.UtcNow);
        db.Context.Courts.Add(court);
        await db.Context.SaveChangesAsync();
        var service = new CourtService(db.Context);

        var result = await service.GetCourtByIdAsync(court.Id, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCourtAsync_WithExistingCourt_UpdatesCourt()
    {
        await using var db = await TestDb.CreateAsync();
        var court = TestData.Court(name: "Court A");
        db.Context.Courts.Add(court);
        await db.Context.SaveChangesAsync();
        var service = new CourtService(db.Context);

        var result = await service.UpdateCourtAsync(court.Id, new CourtUpdateRequest
        {
            Name = " Court B ",
            CourtType = " Tennis ",
            Status = CourtStatus.Maintenance
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Name.Should().Be("Court B");
        result.Value.CourtType.Should().Be("Tennis");
        result.Value.Status.Should().Be(CourtStatus.Maintenance);
        result.Value.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteCourtAsync_WithExistingCourt_SoftDeletesCourt()
    {
        await using var db = await TestDb.CreateAsync();
        var court = TestData.Court();
        db.Context.Courts.Add(court);
        await db.Context.SaveChangesAsync();
        var service = new CourtService(db.Context);

        var result = await service.DeleteCourtAsync(court.Id, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        db.Context.Courts.Single().DeletedAt.Should().NotBeNull();
    }
}

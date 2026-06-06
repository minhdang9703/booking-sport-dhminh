using BookingSport.Api.DTOs.PriceRules;
using BookingSport.Api.Services.PriceRules;
using BookingSport.Api.Tests.TestSupport;
using FluentAssertions;

namespace BookingSport.Api.Tests.Services.PriceRules;

public class PriceRuleServiceTests
{
    [Fact]
    public async Task CreatePriceRuleAsync_WithValidRequest_CreatesRule()
    {
        await using var db = await TestDb.CreateAsync();
        var service = new PriceRuleService(db.Context);

        var result = await service.CreatePriceRuleAsync(new PriceRuleCreateRequest
        {
            Name = " Morning ",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0),
            HourlyPrice = 120_000m,
            IsEnabled = true
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Name.Should().Be("Morning");
        result.Value.HourlyPrice.Should().Be(120_000m);
    }

    [Theory]
    [InlineData("", 8, 10, 100000, PriceRuleResultStatus.BadRequest)]
    [InlineData("Bad time", 10, 8, 100000, PriceRuleResultStatus.BadRequest)]
    [InlineData("Bad price", 8, 10, -1, PriceRuleResultStatus.BadRequest)]
    public async Task CreatePriceRuleAsync_WithInvalidInput_ReturnsBadRequest(
        string name,
        int startHour,
        int endHour,
        int hourlyPrice,
        PriceRuleResultStatus expectedStatus)
    {
        await using var db = await TestDb.CreateAsync();
        var service = new PriceRuleService(db.Context);

        var result = await service.CreatePriceRuleAsync(new PriceRuleCreateRequest
        {
            Name = name,
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(startHour, 0),
            EndTime = new TimeOnly(endHour, 0),
            HourlyPrice = hourlyPrice
        }, CancellationToken.None);

        result.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task CreatePriceRuleAsync_WhenEnabledRuleOverlaps_ReturnsConflict()
    {
        await using var db = await TestDb.CreateAsync();
        db.Context.PriceRules.Add(TestData.PriceRule(startTime: new TimeOnly(8, 0), endTime: new TimeOnly(10, 0)));
        await db.Context.SaveChangesAsync();
        var service = new PriceRuleService(db.Context);

        var result = await service.CreatePriceRuleAsync(new PriceRuleCreateRequest
        {
            Name = "Overlap",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            HourlyPrice = 130_000m,
            IsEnabled = true
        }, CancellationToken.None);

        result.Status.Should().Be(PriceRuleResultStatus.Conflict);
    }

    [Fact]
    public async Task CreatePriceRuleAsync_WhenNewRuleIsDisabled_AllowsOverlap()
    {
        await using var db = await TestDb.CreateAsync();
        db.Context.PriceRules.Add(TestData.PriceRule(startTime: new TimeOnly(8, 0), endTime: new TimeOnly(10, 0)));
        await db.Context.SaveChangesAsync();
        var service = new PriceRuleService(db.Context);

        var result = await service.CreatePriceRuleAsync(new PriceRuleCreateRequest
        {
            Name = "Disabled overlap",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            HourlyPrice = 130_000m,
            IsEnabled = false
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetPriceRulesAsync_FiltersAndSortsRules()
    {
        await using var db = await TestDb.CreateAsync();
        db.Context.PriceRules.AddRange(
            TestData.PriceRule(name: "Late", dayOfWeek: DayOfWeek.Monday, startTime: new TimeOnly(10, 0)),
            TestData.PriceRule(name: "Early", dayOfWeek: DayOfWeek.Monday, startTime: new TimeOnly(8, 0), endTime: new TimeOnly(9, 0)),
            TestData.PriceRule(name: "Disabled", dayOfWeek: DayOfWeek.Monday, isEnabled: false),
            TestData.PriceRule(name: "Tuesday", dayOfWeek: DayOfWeek.Tuesday));
        await db.Context.SaveChangesAsync();
        var service = new PriceRuleService(db.Context);

        var result = await service.GetPriceRulesAsync(new PriceRuleQueryParameters
        {
            DayOfWeek = DayOfWeek.Monday,
            IsEnabled = true
        }, CancellationToken.None);

        result.Select(rule => rule.Name).Should().Equal("Early", "Late");
    }

    [Fact]
    public async Task UpdatePriceRuleAsync_WhenRuleDoesNotExist_ReturnsNotFound()
    {
        await using var db = await TestDb.CreateAsync();
        var service = new PriceRuleService(db.Context);

        var result = await service.UpdatePriceRuleAsync(Guid.NewGuid(), ValidUpdateRequest(), CancellationToken.None);

        result.Status.Should().Be(PriceRuleResultStatus.NotFound);
    }

    [Fact]
    public async Task DeletePriceRuleAsync_WithExistingRule_RemovesRule()
    {
        await using var db = await TestDb.CreateAsync();
        var rule = TestData.PriceRule();
        db.Context.PriceRules.Add(rule);
        await db.Context.SaveChangesAsync();
        var service = new PriceRuleService(db.Context);

        var result = await service.DeletePriceRuleAsync(rule.Id, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        db.Context.PriceRules.Should().BeEmpty();
    }

    private static PriceRuleUpdateRequest ValidUpdateRequest()
    {
        return new PriceRuleUpdateRequest
        {
            Name = "Updated",
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(10, 0),
            HourlyPrice = 100_000m,
            IsEnabled = true
        };
    }
}

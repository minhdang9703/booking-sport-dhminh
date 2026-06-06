using BookingSport.Api.DTOs.Users;
using BookingSport.Api.Entities;
using BookingSport.Api.Enums;
using BookingSport.Api.Services.Users;
using BookingSport.Api.Tests.TestSupport;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;

namespace BookingSport.Api.Tests.Services.Users;

public class UserServiceTests
{
    [Fact]
    public async Task CreateUserAsync_WithValidRequest_CreatesUserWithHashedPassword()
    {
        await using var db = await TestDb.CreateAsync();
        var service = CreateService(db.Context);

        var result = await service.CreateUserAsync(new UserCreateRequest
        {
            FullName = "  Admin User ",
            Email = " ADMIN@Example.COM ",
            Password = "secret123",
            PhoneNumber = " 0900000001 ",
            Role = UserRole.Admin
        }, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Value!.Email.Should().Be("admin@example.com");
        result.Value.FullName.Should().Be("Admin User");
        result.Value.PhoneNumber.Should().Be("0900000001");
        result.Value.Role.Should().Be(UserRole.Admin);

        var user = db.Context.Users.Single();
        user.PasswordHash.Should().NotBe("secret123");
    }

    [Theory]
    [InlineData("", "secret123", "0900000001", UserResultStatus.BadRequest)]
    [InlineData("User", "12345", "0900000001", UserResultStatus.BadRequest)]
    [InlineData("User", "secret123", "", UserResultStatus.BadRequest)]
    public async Task CreateUserAsync_WithInvalidInput_ReturnsBadRequest(
        string fullName,
        string password,
        string phoneNumber,
        UserResultStatus expectedStatus)
    {
        await using var db = await TestDb.CreateAsync();
        var service = CreateService(db.Context);

        var result = await service.CreateUserAsync(new UserCreateRequest
        {
            FullName = fullName,
            Email = "user@example.com",
            Password = password,
            PhoneNumber = phoneNumber
        }, CancellationToken.None);

        result.Status.Should().Be(expectedStatus);
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUserAsync_WhenPhoneExists_ReturnsConflict()
    {
        await using var db = await TestDb.CreateAsync();
        db.Context.Users.Add(TestData.User(phoneNumber: "0900000001"));
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.CreateUserAsync(new UserCreateRequest
        {
            FullName = "User",
            Email = "new@example.com",
            Password = "secret123",
            PhoneNumber = "0900000001"
        }, CancellationToken.None);

        result.Status.Should().Be(UserResultStatus.Conflict);
    }

    [Fact]
    public async Task GetUsersAsync_FiltersByKeywordRoleAndDeletedFlag()
    {
        await using var db = await TestDb.CreateAsync();
        db.Context.Users.AddRange(
            TestData.User(fullName: "Alpha Admin", email: "admin@example.com", phoneNumber: "0900000001", role: UserRole.Admin),
            TestData.User(fullName: "Alpha Customer", email: "customer@example.com", phoneNumber: "0900000002"),
            TestData.User(fullName: "Deleted Admin", email: "deleted@example.com", phoneNumber: "0900000003", role: UserRole.Admin, deletedAt: DateTimeOffset.UtcNow));
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.GetUsersAsync(new UserQueryParameters
        {
            Keyword = "alpha",
            Role = UserRole.Admin
        }, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].FullName.Should().Be("Alpha Admin");

        var withDeleted = await service.GetUsersAsync(new UserQueryParameters
        {
            Role = UserRole.Admin,
            IncludeDeleted = true
        }, CancellationToken.None);
        withDeleted.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateUserAsync_WhenPhoneBelongsToAnotherUser_ReturnsConflict()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User(phoneNumber: "0900000001");
        db.Context.Users.AddRange(user, TestData.User(email: "other@example.com", phoneNumber: "0900000002"));
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.UpdateUserAsync(user.Id, new UserUpdateRequest
        {
            FullName = "Updated",
            PhoneNumber = "0900000002",
            Role = UserRole.Customer
        }, CancellationToken.None);

        result.Status.Should().Be(UserResultStatus.Conflict);
    }

    [Fact]
    public async Task DeleteUserAsync_WhenDeletingCurrentUser_ReturnsBadRequest()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.DeleteUserAsync(user.Id, user.Id, CancellationToken.None);

        result.Status.Should().Be(UserResultStatus.BadRequest);
    }

    [Fact]
    public async Task DeleteUserAsync_WithExistingUser_SoftDeletesUser()
    {
        await using var db = await TestDb.CreateAsync();
        var user = TestData.User();
        db.Context.Users.Add(user);
        await db.Context.SaveChangesAsync();
        var service = CreateService(db.Context);

        var result = await service.DeleteUserAsync(user.Id, Guid.NewGuid(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        db.Context.Users.Single().DeletedAt.Should().NotBeNull();
    }

    private static UserService CreateService(BookingSport.Api.Data.AppDbContext dbContext)
    {
        return new UserService(dbContext, new PasswordHasher<User>());
    }
}

using BookingSport.Api.Data;
using BookingSport.Api.DTOs.Users;
using BookingSport.Api.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookingSport.Api.Services.Users;

public class UserService(
    AppDbContext dbContext,
    PasswordHasher<User> passwordHasher) : IUserService
{
    public async Task<IReadOnlyList<UserResponse>> GetUsersAsync(
        UserQueryParameters query,
        CancellationToken cancellationToken)
    {
        var usersQuery = dbContext.Users.AsNoTracking().AsQueryable();

        if (!query.IncludeDeleted)
        {
            usersQuery = usersQuery.Where(user => user.DeletedAt == null);
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim().ToLower();
            usersQuery = usersQuery.Where(user =>
                user.FullName.ToLower().Contains(keyword) ||
                user.Email.ToLower().Contains(keyword) ||
                (user.PhoneNumber != null && user.PhoneNumber.Contains(keyword)));
        }

        if (query.Role.HasValue)
        {
            usersQuery = usersQuery.Where(user => user.Role == query.Role.Value);
        }

        return await usersQuery
            .OrderByDescending(user => user.CreatedAt)
            .Select(user => MapUserResponse(user))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserResponse?> GetUserByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == id && user.DeletedAt == null)
            .Select(user => MapUserResponse(user))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserResult<UserResponse>> CreateUserAsync(
        UserCreateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return UserResult<UserResponse>.BadRequest("FullName is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
        {
            return UserResult<UserResponse>.BadRequest("Password must have at least 6 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return UserResult<UserResponse>.BadRequest("PhoneNumber is required.");
        }

        var phoneNumber = request.PhoneNumber.Trim();
        var phoneExists = await dbContext.Users
            .AnyAsync(user => user.PhoneNumber == phoneNumber, cancellationToken);

        if (phoneExists)
        {
            return UserResult<UserResponse>.Conflict("PhoneNumber is already used.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLower(),
            PhoneNumber = phoneNumber,
            Role = request.Role,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return UserResult<UserResponse>.Success(MapUserResponse(user));
    }

    public async Task<UserResult<UserResponse>> UpdateUserAsync(
        Guid id,
        UserUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return UserResult<UserResponse>.BadRequest("FullName is required.");
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(existingUser => existingUser.Id == id && existingUser.DeletedAt == null, cancellationToken);

        if (user is null)
        {
            return UserResult<UserResponse>.NotFound("User was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            return UserResult<UserResponse>.BadRequest("PhoneNumber is required.");
        }

        var phoneNumber = request.PhoneNumber.Trim();
        var phoneExists = await dbContext.Users.AnyAsync(otherUser =>
            otherUser.Id != user.Id &&
            otherUser.PhoneNumber == phoneNumber,
            cancellationToken);

        if (phoneExists)
        {
            return UserResult<UserResponse>.Conflict("PhoneNumber is already used.");
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = phoneNumber;
        user.Role = request.Role;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return UserResult<UserResponse>.Success(MapUserResponse(user));
    }

    public async Task<UserResult<bool>> DeleteUserAsync(
        Guid id,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (id == currentUserId)
        {
            return UserResult<bool>.BadRequest("Current admin user cannot be deleted.");
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(existingUser => existingUser.Id == id && existingUser.DeletedAt == null, cancellationToken);

        if (user is null)
        {
            return UserResult<bool>.NotFound("User was not found.");
        }

        user.DeletedAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return UserResult<bool>.Success(true);
    }

    private static UserResponse MapUserResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}

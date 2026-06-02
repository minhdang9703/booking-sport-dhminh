using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Users;

public class UserUpdateRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public UserRole Role { get; set; }
}

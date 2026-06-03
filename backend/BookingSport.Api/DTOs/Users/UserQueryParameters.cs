using BookingSport.Api.Enums;

namespace BookingSport.Api.DTOs.Users;

public class UserQueryParameters
{
    public string? Keyword { get; set; }
    public UserRole? Role { get; set; }
    public bool IncludeDeleted { get; set; }
}
